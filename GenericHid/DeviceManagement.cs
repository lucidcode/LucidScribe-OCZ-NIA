///  <summary>
///  For detecting devices and receiving device notifications.
///  </summary>

using System;
using System.Diagnostics;
using System.Runtime.InteropServices; 
using System.Windows.Forms;

namespace GenericHid
{
    public sealed partial class DeviceManagement  
    {         
        //  Used in error messages:
        
        private const String MODULE_NAME = "DeviceManagement"; 
        
        //  For viewing results of API calls in debug.write statements:
        
        private Debugging MyDebugging = new Debugging(); 
        
        ///  <summary>
        ///  Compares two device path names. Used to find out if the device name 
        ///  of a recently attached or removed device matches the name of a 
        ///  device the application is communicating with.
        ///  </summary>
        ///  
        ///  <param name="m"> a WM_DEVICECHANGE message. A call to RegisterDeviceNotification
        ///  causes WM_DEVICECHANGE messages to be passed to an OnDeviceChange routine. </param>
        ///  <param name="mydevicePathName"> a device pathname returned by SetupDiGetDeviceInterfaceDetail
        ///  in an SP_DEVICE_INTERFACE_DETAIL_DATA structure</param>
        ///  
        ///  <returns>
        ///  True if the names match, False if not.
        ///  </returns>

        public Boolean DeviceNameMatch(Message m, String mydevicePathName) 
        {             
            try 
            { 
                DEV_BROADCAST_DEVICEINTERFACE_1 DevBroadcastDeviceInterface = new DEV_BROADCAST_DEVICEINTERFACE_1(); 
                DEV_BROADCAST_HDR DevBroadcastHeader = new DEV_BROADCAST_HDR(); 
                
                //  The LParam parameter of Message is a pointer to a DEV_BROADCAST_HDR structure.
                
                Marshal.PtrToStructure( m.LParam, DevBroadcastHeader ); 
                
                if ( ( DevBroadcastHeader.dbch_devicetype == DBT_DEVTYP_DEVICEINTERFACE ) ) 
                {                     
                    //  The dbch_devicetype parameter indicates that the event applies to a device interface.
                    //  So the structure in LParam is actually a DEV_BROADCAST_INTERFACE structure, 
                    //  which begins with a DEV_BROADCAST_HDR.
                    
                    //  Obtain the number of characters in dbch_name by subtracting the 32 bytes
                    //  in the strucutre that are not part of dbch_name and dividing by 2 because there are 
                    //  2 bytes per character.
                    
                    Int32 stringSize = Convert.ToInt32( ( DevBroadcastHeader.dbch_size - 32 ) / 2 ); 
                    
                    //  The dbcc_name parameter of DevBroadcastDeviceInterface contains the device name. 
                    //  Trim dbcc_name to match the size of the String.
                    
                    DevBroadcastDeviceInterface.dbcc_name = new char[ stringSize ]; 
                    
                    //  Marshal data from the unmanaged block pointed to by m.LParam 
                    //  to the managed object DevBroadcastDeviceInterface.
                    
                    Marshal.PtrToStructure( m.LParam, DevBroadcastDeviceInterface ); 
                    
                    //  Store the device name in a String.
                    
                    String deviceNameString = new String( DevBroadcastDeviceInterface.dbcc_name, 0, stringSize ); 
                    
                    Debug.WriteLine( "Device Name =      " + deviceNameString ); 
                    Debug.WriteLine( "myDevicePathName = " + mydevicePathName ); 
                    
                    //  Compare the name of the newly attached device with the name of the device 
                    //  the application is accessing (mydevicePathName).
                    //  Set ignorecase True.
                    
                    if ( ( String.Compare( deviceNameString, mydevicePathName, true ) == 0 ) ) 
                    {                         
                        //  The name matches.
                        
                        return true; 
                    } 
                    else 
                    {                         
                        //  It's a different device.
                        
                        return false; 
                    } 
                }                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( MODULE_NAME, ex ); 
            } 
            
            return false;
        }         
        
        ///  <summary>
        ///  Uses SetupDi API functions to retrieve the device path name of an
        ///  attached device that belongs to an interface class.
        ///  </summary>
        ///  
        ///  <param name="myGuid"> an interface class GUID. </param>
        ///  <param name="devicePathName"> a pointer to an array of strings that 
        ///  will contain the device path names of attached devices. </param>
        ///  
        ///  <returns>
        ///  True if at least one device is found, False if not. 
        ///  </returns>

        public Boolean FindDeviceFromGuid(System.Guid myGuid, ref String[] devicePathName) 
        {
            String apiFunction = "";
            Boolean deviceFound = false; 
            IntPtr deviceInfoSet = new System.IntPtr();
            bool is64Bit = false;
            Boolean lastDevice = false; 
            Int32 bufferSize = 0; 
            Int32 memberIndex = 0;
            SP_DEVICE_INTERFACE_DATA MyDeviceInterfaceData = new DeviceManagement.SP_DEVICE_INTERFACE_DATA(); 
            SP_DEVICE_INTERFACE_DETAIL_DATA MyDeviceInterfaceDetailData = new DeviceManagement.SP_DEVICE_INTERFACE_DETAIL_DATA();             
            Boolean result = false; 
            Boolean success = false;

            try
            {             
                if (System.IntPtr.Size == 8)
                    is64Bit = true;

                //  ***
                //  API function: SetupDiGetClassDevs

                //  Purpose: 
                //  Retrieves a device information set for a specified group of devices.
                //  SetupDiEnumDeviceInterfaces uses the device information set.

                //  Accepts: 
                //  An interface class GUID
                //  Null to retrieve information for all device instances
                //  An optional handle to a top-level window (unused here)
                //  Flags to limit the returned information to currently present devices 
                //  and devices that expose interfaces in the class specified by the GUID.

                //  Returns:
                //  A handle to a device information set for the devices.
                //  ***

                deviceInfoSet = SetupDiGetClassDevs(ref myGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                apiFunction = "SetupDiClassDevs";
                Debug.WriteLine(MyDebugging.ResultOfAPICall(apiFunction));

                deviceFound = false;             

                do
                {
                    //  Begin with 0 and increment through the device information set until
                    //  no more devices are available.

                    //  The cbSize element of the MyDeviceInterfaceData structure must be set to
                    //  the structure's size in bytes. The size is 28 bytes.

                    // Alternative for 32-bit code:
                    // MyDeviceInterfaceData.cbSize = Marshal.SizeOf(MyDeviceInterfaceData);

                    if (!is64Bit)
                     {
                         MyDeviceInterfaceData.cbSize = 28;
                     }
                     else 
                     {
                         MyDeviceInterfaceData.cbSize = 32;
                     }
                    
                    //  ***
                    //  API function: 
                    //  SetupDiEnumDeviceInterfaces()

                    //  Purpose: Retrieves a handle to a SP_DEVICE_INTERFACE_DATA 
                    //  structure for a device.
                    //  On return, MyDeviceInterfaceData contains the handle to a
                    //  SP_DEVICE_INTERFACE_DATA structure for a detected device.

                    //  Accepts:
                    //  A DeviceInfoSet returned by SetupDiGetClassDevs.
                    //  An interface class GUID.
                    //  An index to specify a device in a device information set.
                    //  A pointer to a handle to a SP_DEVICE_INTERFACE_DATA structure for a device.

                    //  Returns:
                    //  Non-zero on success, zero on True.
                    //  ***

                    result = SetupDiEnumDeviceInterfaces(deviceInfoSet, 0, ref myGuid, memberIndex, ref MyDeviceInterfaceData);

                    apiFunction = "SetupDiEnumDeviceInterfaces";
                    Debug.WriteLine(MyDebugging.ResultOfAPICall(apiFunction));

                    //  Find out if a device information set was retrieved.

                    if ((result == false))
                    {
                        lastDevice = true;
                    }
                    else
                    {
                        //  A device is present.

                        Debug.WriteLine("  DeviceInfoSet for device #" + Convert.ToString(memberIndex) + ": ");
                        Debug.WriteLine("  cbSize = " + Convert.ToString(MyDeviceInterfaceData.cbSize));
                        Debug.WriteLine("  InterfaceclassGuid = " + MyDeviceInterfaceData.InterfaceClassGuid.ToString());                                               
                        Debug.WriteLine("  Flags = " + Convert.ToString(MyDeviceInterfaceData.Flags, 16));
                        
                        //  ***
                        //  API function: 
                        //  SetupDiGetDeviceInterfaceDetail()

                        //  Purpose:
                        //  Retrieves an SP_DEVICE_INTERFACE_DETAIL_DATA structure
                        //  containing information about a device.
                        //  To retrieve the information, call this function twice.
                        //  The first time returns the size of the structure.
                        //  The second time returns a pointer to the data.

                        //  Accepts:
                        //  A DeviceInfoSet returned by SetupDiGetClassDevs
                        //  An SP_DEVICE_INTERFACE_DATA structure returned by SetupDiEnumDeviceInterfaces
                        //  A pointer to an SP_DEVICE_INTERFACE_DETAIL_DATA structure to receive information 
                        //  about the specified interface.
                        //  The size of the SP_DEVICE_INTERFACE_DETAIL_DATA structure.
                        //  A pointer to a variable that will receive the returned required size of the 
                        //  SP_DEVICE_INTERFACE_DETAIL_DATA structure.
                        //  A pointer to an SP_DEVINFO_DATA structure to receive information about the device.

                        //  Returns:
                        //  Non-zero on success, zero on failure.
                        //  ***

                        //MyDeviceInterfaceDetailData = ( ( DeviceManagement.SP_DEVICE_INTERFACE_DETAIL_DATA )( null ) ); 

                        success = SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref MyDeviceInterfaceData, IntPtr.Zero, 0, ref bufferSize, IntPtr.Zero);

                        apiFunction = "SetupDiGetDeviceInterfaceDetail";
                        Debug.WriteLine(MyDebugging.ResultOfAPICall(apiFunction));
                        Debug.WriteLine("  (OK to say too small)");
                        Debug.WriteLine("  Required buffer size for the data: " + bufferSize);

                        //  Store the structure's size.

                        MyDeviceInterfaceDetailData.cbSize = Marshal.SizeOf(MyDeviceInterfaceDetailData);

                        //  Allocate memory for the MyDeviceInterfaceDetailData Structure using the returned buffer size.

                        IntPtr detailDataBuffer = Marshal.AllocHGlobal(bufferSize);

                        //  Store cbSize in the first 4 bytes of the array                   
                        
                        if (!is64Bit)
                        {
                            Marshal.WriteInt32(detailDataBuffer, 4 + Marshal.SystemDefaultCharSize);
                        }
                        else
                        {
                            Marshal.WriteInt32(detailDataBuffer, 8);
                        }

                        Debug.WriteLine("cbsize = " + MyDeviceInterfaceDetailData.cbSize);

                        //  Call SetupDiGetDeviceInterfaceDetail again.
                        //  This time, pass a pointer to DetailDataBuffer
                        //  and the returned required buffer size.

                        success = SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref MyDeviceInterfaceData, detailDataBuffer, bufferSize, ref bufferSize, IntPtr.Zero);

                        apiFunction = " Result of second call: ";
                        Debug.WriteLine(MyDebugging.ResultOfAPICall(apiFunction));
                        Debug.WriteLine("  MyDeviceInterfaceDetailData.cbSize: " + Convert.ToString(MyDeviceInterfaceDetailData.cbSize));

                        //  Skip over cbsize (4 bytes) to get the address of the devicePathName.

                        IntPtr pdevicePathName = new IntPtr(detailDataBuffer.ToInt32() + 4);

                        //  Get the String containing the devicePathName.

                        devicePathName[memberIndex] = Marshal.PtrToStringAuto(pdevicePathName);

                        Debug.WriteLine("Device Path = " + devicePathName[memberIndex]);
                        Debug.WriteLine("Device Path Length= " + devicePathName[memberIndex].Length);

                        //  Free the memory allocated previously by AllocHGlobal.

                        Marshal.FreeHGlobal(detailDataBuffer);
                        deviceFound = true;
                    }

                    memberIndex = memberIndex + 1;
                }
                while (!lastDevice == true);

                //  Trim the array to the number of devices found.
               
                String[] tempArray = new String[memberIndex - 1];
                System.Array.Copy(devicePathName, tempArray, Math.Min(devicePathName.Length, tempArray.Length));
                devicePathName = tempArray;

                Debug.WriteLine("Number of HIDs found = " + Convert.ToString(memberIndex - 1));

                //  ***
                //  API function:
                //  SetupDiDestroyDeviceInfoList

                //  Purpose:
                //  Frees the memory reserved for the DeviceInfoSet returned by SetupDiGetClassDevs.

                //  Accepts:
                //  A DeviceInfoSet returned by SetupDiGetClassDevs.

                //  Returns:
                //  True on success, False on failure.
                //  ***

                SetupDiDestroyDeviceInfoList(deviceInfoSet);

                apiFunction = "DestroyDeviceInfoList";
                Debug.WriteLine(MyDebugging.ResultOfAPICall(apiFunction));

                return deviceFound;
            }
            catch (Exception ex)
            {
                DisplayException( MODULE_NAME, ex );
                return false;
            }                          }         
        
        ///  <summary>
        ///  Request to receive a notification when a device is attached or removed.
        ///  </summary>
        ///  
        ///  <param name="devicePathName"> a handle to a device.</param>
        ///  <param name="formHandle"> a handle to the window that will receive device events. </param>
        ///  <param name="classGuid"> an interface class GUID. </param>
        ///  <param name="deviceNotificationHandle"> the retrieved handle. (Used when
        ///  requesting to stop receiving notifications.) </param>
        ///  
        ///  <returns>
        ///  True on success, False on failure.
        ///  </returns>

        public Boolean RegisterForDeviceNotifications(String devicePathName, IntPtr formHandle, Guid classGuid, ref IntPtr deviceNotificationHandle) 
        {             
            //  A DEV_BROADCAST_DEVICEINTERFACE header holds information about the request.
            
            DEV_BROADCAST_DEVICEINTERFACE DevBroadcastDeviceInterface = new DEV_BROADCAST_DEVICEINTERFACE(); 
            IntPtr devBroadcastDeviceInterfaceBuffer = new System.IntPtr(); 
            Int32 size = 0; 
            
            try 
            { 
                //  Set the parameters in the DEV_BROADCAST_DEVICEINTERFACE structure.                
                //  Set the size.
                
                size = Marshal.SizeOf( DevBroadcastDeviceInterface ); 
                DevBroadcastDeviceInterface.dbcc_size = size; 
                
                //  Request to receive notifications about a class of devices.
                
                DevBroadcastDeviceInterface.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE; 
                
                DevBroadcastDeviceInterface.dbcc_reserved = 0; 
                
                //  Specify the interface class to receive notifications about.
                
                DevBroadcastDeviceInterface.dbcc_classguid = classGuid; 
                
                //  Allocate memory for the buffer that holds the DEV_BROADCAST_DEVICEINTERFACE structure.
                
                devBroadcastDeviceInterfaceBuffer = Marshal.AllocHGlobal( size ); 
                
                //  Copy the DEV_BROADCAST_DEVICEINTERFACE structure to the buffer.
                //  Set fDeleteOld True to prevent memory leaks.
                
                Marshal.StructureToPtr( DevBroadcastDeviceInterface, devBroadcastDeviceInterfaceBuffer, true ); 
                
                //  ***
                //  API function: 
                //  RegisterDeviceNotification
                
                //  Purpose:
                //  Request to receive notification messages when a device in an interface class
                //  is attached or removed.
                
                //  Accepts: 
                //  A handle to the window that will receive device events
                //  A pointer to a DEV_BROADCAST_DEVICEINTERFACE to specify the type of 
                //  device to send notifications for,
                //  DEVICE_NOTIFY_WINDOW_HANDLE to indicate that Handle is a window handle.
                
                //  Returns:
                //  A device notification handle or NULL on failure.
                //  ***
                
                deviceNotificationHandle = RegisterDeviceNotification( formHandle, devBroadcastDeviceInterfaceBuffer, DEVICE_NOTIFY_WINDOW_HANDLE ); 
                
                //  Marshal data from the unmanaged block DevBroadcastDeviceInterfaceBuffer to
                //  the managed object DevBroadcastDeviceInterface
                
                Marshal.PtrToStructure( devBroadcastDeviceInterfaceBuffer, DevBroadcastDeviceInterface ); 
                
                //  Free the memory allocated previously by AllocHGlobal.
                
                Marshal.FreeHGlobal( devBroadcastDeviceInterfaceBuffer ); 
                
                //  Find out if RegisterDeviceNotification was successful.
                
                if ( ( deviceNotificationHandle.ToInt32() == IntPtr.Zero.ToInt32() ) ) 
                { 
                    Debug.WriteLine( "RegisterDeviceNotification error" ); 
                    return false; 
                } 
                else 
                { 
                    return true; 
                } 
                
            } 
            catch ( Exception ex ) 
            {
              DisplayException(MODULE_NAME, ex);
              return false;
            }         
        }         
        
        ///  <summary>
        ///  Requests to stop receiving notification messages when a device in an 
        ///  interface class is attached or removed.
        ///  </summary>
        ///  
        ///  <param name="deviceNotificationHandle"> a handle returned previously by
        ///  RegisterDeviceNotification  </param>

        public void StopReceivingDeviceNotifications(IntPtr deviceNotificationHandle) 
        {             
            try 
            { 
                //  ***
                //  API function: UnregisterDeviceNotification
                
                //  Purpose: Stop receiving notification messages.
                
                //  Accepts: a handle returned previously by RegisterDeviceNotification  
                
                //  Returns: True on success, False on failure.
                //  ***
                
                //  Ignore failures.
                
                UnregisterDeviceNotification( deviceNotificationHandle );                 
            } 
            catch ( Exception ex ) 
            { 
                DisplayException( MODULE_NAME, ex ); 
            } 
        }         
        
        ///  <summary>
        ///  Provides a central mechanism for exception handling.
        ///  Displays a message box that describes the exception.
        ///  </summary>
        ///  
        ///  <param name="moduleName">  the module where the exception occurred. </param>
        ///  <param name="e"> the exception </param>
        
        public static void DisplayException( String moduleName, Exception e ) 
        {             
            String message = null; 
            String caption = null; 
            
            //  Create an error message.
            
            message = "Exception: " + e.Message + System.Environment.NewLine + "Module: " + moduleName + System.Environment.NewLine + "Method: " + e.TargetSite.Name; 
            
            //caption = "Unexpected Exception"; 
            
            //MessageBox.Show( message, caption, MessageBoxButtons.OK ); 
            Debug.Write( message );
            throw new Exception(message);
        } 
    }   
} 
