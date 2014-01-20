using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.IO.Ports;
using System.Windows.Forms;
using System.Drawing;
using GenericHid;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace lucidcode.LucidScribe.Plugin.OpenEEG
{
    public static class Device
    {
        static bool Initialized;
        static bool InitError;
        static int[] eegChannels;
        static double eegValue;

        static double eegTicks;
        static bool clearRaw;

        static GenericHid.Hid MyHid = new GenericHid.Hid();
        static bool NiaDetected = false;
        static string NiaPathName = string.Empty;

        static SafeFileHandle hidHandle;
        static SafeFileHandle readHandle;
        static SafeFileHandle writeHandle;
        static IntPtr deviceNotificationHandle;

        static Boolean Cancelled = false;

        public static string Algorithm = "REM Detection";
        public static int BlinkInterval = 28;

        public static EventHandler<EventArgs> EEGChanged;
        static PortForm formPort = new PortForm();

        public static Boolean Initialize()
        {
            if (!Initialized & !InitError)
            {
                try
                {
                  FindNia(formPort.Handle);

                  if (NiaDetected)
                  {
                    Thread clock = new Thread(Ticker);
                    clock.Start();
                  }
                  else
                  { 
                    MessageBox.Show("Failed to detect the OCZ NIA.", "LucidScribe.InitializePlugin()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    InitError = true;
                  }
                }
                catch (Exception ex)
                {
                  if (!InitError)
                  {
                    MessageBox.Show("Failed to initialize the OCZ NIA plugin: " + ex.Message, "LucidScribe.InitializePlugin()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  }
                  InitError = true;
                }

                Initialized = true;
            }
            return true;
        }

        private static void Ticker()
        {
          try
          {
            do
            {
              ReadFromNiaSync();
              Thread.Sleep(1);
              if (Cancelled) break;
            }
            while (true);
          }
          catch (Exception ex)
          {
            Cancelled = true;
            MessageBox.Show("The OCZ NIA caused raised an exception: " + ex.Message, "LucidScribe.InitializePlugin()", MessageBoxButtons.OK, MessageBoxIcon.Error);
          }
        }

        private static void ReadFromNiaSync()
        {
          Byte[] inputReportBuffer = new Byte[56];
          Boolean success = false;

          GenericHid.Hid.InputReportViaInterruptTransfer myInputReport = new GenericHid.Hid.InputReportViaInterruptTransfer();
          myInputReport.Read(hidHandle, readHandle, writeHandle, ref NiaDetected, ref inputReportBuffer, ref success);

          if (success)
            Interpret(inputReportBuffer);
        }

        private static void Interpret(Byte[] data)
        {
          // interpretation taken from niawiimote hack						
          long validPackets = data[55];
          long packetTimer = data[54] * 256 + data[53] - validPackets;

          for (long index = 0; index <= (validPackets - 1); index++)
          {
            long timerPosition = (packetTimer + index);
            long rawData = data[index * 3 + 1] * 1 + data[index * 3 + 2] * 256 + data[index * 3 + 3] * 65535;
            rawData = (rawData - 8388480); // MaxValue detected (0xFFFF00) divided/2

            int currentValue = Convert.ToInt32(rawData / 256);
            if (clearRaw)
            {
              clearRaw = false;
              eegValue = 0;
              eegTicks = 0;
            }
            eegValue += currentValue;
            eegTicks++;

            if (EEGChanged != null)
            {
              EEGChanged((object)currentValue, null);
            }
          }
        }

        public static bool FindNia(IntPtr Handle)
        {
          bool success = false;

          try
          {
            // Get the guid for the system hid class
            Guid hidGuid = Guid.Empty;
            GenericHid.Hid.HidD_GetHidGuid(ref hidGuid);

            // Find all devices of type hid
            string[] deviceCollection = new String[128];
            DeviceManagement deviceManagement = new DeviceManagement();
            bool devicesFound = deviceManagement.FindDeviceFromGuid(hidGuid, ref deviceCollection);

            // Did we find any hid devices ?            
            if (devicesFound)
            {
              int memberIndex = 0;
              do
              {
                // try to get a handle on the current hid device in the list
                hidHandle = GenericHid.FileIO.CreateFile(deviceCollection[memberIndex], 0, GenericHid.FileIO.FILE_SHARE_READ | GenericHid.FileIO.FILE_SHARE_WRITE, null, GenericHid.FileIO.OPEN_EXISTING, 0, 0);

                if (!hidHandle.IsInvalid)
                {
                  // Set the Size property of DeviceAttributes to the number of bytes in the structure.
                  MyHid.DeviceAttributes.Size = Marshal.SizeOf(MyHid.DeviceAttributes);

                  // try to get the hid's information
                  success = GenericHid.Hid.HidD_GetAttributes(hidHandle, ref MyHid.DeviceAttributes);
                  if (success)
                  {
                    if ((MyHid.DeviceAttributes.VendorID == 4660) & (MyHid.DeviceAttributes.ProductID == 0))
                    {
                      NiaDetected = true;

                      // Save the DevicePathName for OnDeviceChange().
                      NiaPathName = deviceCollection[memberIndex];
                    }
                    else
                    {
                      NiaDetected = false;
                      hidHandle.Close();
                    }
                  }
                  else
                  {
                    NiaDetected = false;
                    hidHandle.Close();
                  }
                }

                //  Keep looking until we find the device or there are no devices left to examine.
                memberIndex = memberIndex + 1;

              } while (!((NiaDetected | (memberIndex == deviceCollection.Length))));

              // Did we find a NIA ?
              if (NiaDetected)
              {
                // The device was detected.
                // Register to receive notifications if the device is removed or attached.
                success = deviceManagement.RegisterForDeviceNotifications(NiaPathName, Handle, hidGuid, ref deviceNotificationHandle);

                if (success)
                {
                  //  Get handles to use in requesting Input and Output reports.
                  readHandle = GenericHid.FileIO.CreateFile(NiaPathName, GenericHid.FileIO.GENERIC_READ, GenericHid.FileIO.FILE_SHARE_READ | GenericHid.FileIO.FILE_SHARE_WRITE, null, GenericHid.FileIO.OPEN_EXISTING, GenericHid.FileIO.FILE_FLAG_OVERLAPPED, 0);

                  if (!readHandle.IsInvalid)
                  {
                    writeHandle = GenericHid.FileIO.CreateFile(NiaPathName, GenericHid.FileIO.GENERIC_WRITE, GenericHid.FileIO.FILE_SHARE_READ | GenericHid.FileIO.FILE_SHARE_WRITE, null, GenericHid.FileIO.OPEN_EXISTING, 0, 0);
                    MyHid.FlushQueue(readHandle);
                  }
                }
              }
            }

            return NiaDetected;
          }
          catch (Exception ex)
          {
            throw ex;
          }
        }

        private static void DisposeNIA()
        {
          try
          {
            //  Close open handles to the device.
            if (!(hidHandle == null))
            {
              if (!(hidHandle.IsInvalid))
              {
                hidHandle.Close();
              }
            }

            if (!(readHandle == null))
            {
              if (!(readHandle.IsInvalid))
              {
                readHandle.Close();
              }
            }

            if (!(writeHandle == null))
            {
              if (!(writeHandle.IsInvalid))
              {
                writeHandle.Close();
              }
            }

            //  Stop receiving notifications.
            DeviceManagement deviceManagement = new DeviceManagement();
            deviceManagement.StopReceivingDeviceNotifications(deviceNotificationHandle);
          }
          catch (Exception ex)
          {
            throw ex;
          }
        }

        public static void Dispose()
        {
            if (Initialized)
            {
                Cancelled = true;
                Initialized = false;
                DisposeNIA();
            }
        }

        public static Double GetEEG()
        {
          if (eegTicks == 0) return 0;
          return (eegValue / eegTicks);
        }

        public static void ClearEEG()
        {
          clearRaw = true;
        }
    }

    namespace EEG
    {
        public class PluginHandler : lucidcode.LucidScribe.Interface.LucidPluginBase
        {

            public override string Name
            {
                get
                {
                    return "OCZ NIA";
                }
            }

            public override bool Initialize()
            {
                try
                {
                    return Device.Initialize();
                }
                catch (Exception ex)
                {
                    throw (new Exception("The '" + Name + "' plugin failed to initialize: " + ex.Message));
                }
            }

            public override double Value
            {
                get
                {
                    double tempValue = Device.GetEEG();
                    Device.ClearEEG();
                    if (tempValue > 999) { tempValue = 999; }
                    if (tempValue < 0) { tempValue = 0; }
                    return tempValue;
                }
            }

            public override void Dispose()
            {
                Device.Dispose();
            }
        }
    }

    namespace RAW
    {
      public class PluginHandler : lucidcode.LucidScribe.Interface.ILluminatedPlugin
      {

        private double m_dblValue = 256;

        public string Name
        {
          get
          {
            return "OCZ NIA RAW";
          }
        }

        public bool Initialize()
        {
          try
          {
            bool initialized = Device.Initialize();
            Device.EEGChanged += EEGChanged;
            return initialized;
          }
          catch (Exception ex)
          {
            throw (new Exception("The '" + Name + "' plugin failed to initialize: " + ex.Message));
          }
        }

        public event Interface.SenseHandler Sensed;
        public void EEGChanged(object sender, EventArgs e)
        {
          if (ClearTicks)
          {
            ClearTicks = false;
            TickCount = "";
          }
          TickCount += sender + ",";

          if (ClearBuffer)
          {
            ClearBuffer = false;
            BufferData = "";
          }
          BufferData += sender + ",";
        }

        public void Dispose()
        {
          Device.EEGChanged -= EEGChanged;
          Device.Dispose();
        }

        public Boolean isEnabled = false;
        public Boolean Enabled
        {
          get
          {
            return isEnabled;
          }
          set
          {
            isEnabled = value;
          }
        }

        public Color PluginColor = Color.White;
        public Color Color
        {
          get
          {
            return Color;
          }
          set
          {
            Color = value;
          }
        }

        private Boolean ClearTicks = false;
        public String TickCount = "";
        public String Ticks
        {
          get
          {
            ClearTicks = true;
            return TickCount;
          }
          set
          {
            TickCount = value;
          }
        }

        private Boolean ClearBuffer = false;
        public String BufferData = "";
        public String Buffer
        {
          get
          {
            ClearBuffer = true;
            return BufferData;
          }
          set
          {
            BufferData = value;
          }
        }

        int lastHour;
        public int LastHour
        {
          get
          {
            return lastHour;
          }
          set
          {
            lastHour = value;
          }
        }
      }
    }

    namespace REM
    {
      public class PluginHandler : lucidcode.LucidScribe.Interface.LucidPluginBase
      {

        static int TicksSinceLastArtifact = 0;
        static int TicksAbove = 0;

        public override string Name
        {
          get
          {
            return "OCZ NIA REM";
          }
        }

        public override bool Initialize()
        {
          try
          {
            return Device.Initialize();
          }
          catch (Exception ex)
          {
            throw (new Exception("The '" + Name + "' plugin failed to initialize: " + ex.Message));
          }
        }

        List<int> m_arrHistory = new List<int>();

        public override double Value
        {
          get
          {

            if (Device.Algorithm == "REM Detection")
            {
              // Update the mem list
              m_arrHistory.Add(Convert.ToInt32(Device.GetEEG()));
              if (m_arrHistory.Count > 512) { m_arrHistory.RemoveAt(0); }

              // Check for blinks
              int intBlinks = 0;
              bool boolBlinking = false;

              int intBelow = 0;
              int intAbove = 0;

              bool boolDreaming = false;
              foreach (Double dblValue in m_arrHistory)
              {
                if (dblValue > 600)
                {
                  intAbove += 1;
                  intBelow = 0;
                }
                else
                {
                  intBelow += 1;
                  intAbove = 0;
                }

                if (!boolBlinking)
                {
                  if (intAbove >= 1)
                  {
                    boolBlinking = true;
                    intBlinks += 1;
                    intAbove = 0;
                    intBelow = 0;
                  }
                }
                else
                {
                  if (intBelow >= Device.BlinkInterval)
                  {
                    boolBlinking = false;
                    intBelow = 0;
                    intAbove = 0;
                  }
                  else
                  {
                    if (intAbove >= 12)
                    {
                      // reset
                      boolBlinking = false;
                      intBlinks = 0;
                      intBelow = 0;
                      intAbove = 0;
                    }
                  }
                }

                if (intBlinks > 6)
                {
                  boolDreaming = true;
                  break;
                }

                if (intAbove > 12)
                { // reset
                  boolBlinking = false;
                  intBlinks = 0;
                  intBelow = 0;
                  intAbove = 0; ;
                }
                if (intBelow > 80)
                { // reset
                  boolBlinking = false;
                  intBlinks = 0;
                  intBelow = 0;
                  intAbove = 0; ;
                }
              }

              if (boolDreaming)
              { return 888; }

              if (intBlinks > 10) { intBlinks = 10; }
              return intBlinks * 100;
            }
            else if (Device.Algorithm == "Motion Detection")
            {
                if (Device.GetEEG() > 980)
                {
                  TicksAbove++;
                  if (TicksAbove > 5)
                  {
                    TicksAbove = 0;
                    TicksSinceLastArtifact = 0;
                    if (TicksSinceLastArtifact > 19200)
                    {
                      return 888;
                    }
                  }
                }

              TicksSinceLastArtifact++;
              return 0;
            }

            return 0;
          }
        }

        public override void Dispose()
        {
          Device.Dispose();
        }
      }
    }

}
