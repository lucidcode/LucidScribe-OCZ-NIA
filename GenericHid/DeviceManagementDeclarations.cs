using System;
using System.Runtime.InteropServices;

namespace GenericHid
{
    public sealed partial class DeviceManagement  
    {         
        // from dbt.h
        
        public const Int32 DBT_DEVICEARRIVAL = 0X8000; 
        public const Int32 DBT_DEVICEREMOVECOMPLETE = 0X8004; 
        public const Int32 DBT_DEVTYP_DEVICEINTERFACE = 5; 
        public const Int32 DBT_DEVTYP_HANDLE = 6; 
        public const Int32 DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4; 
        public const Int32 DEVICE_NOTIFY_SERVICE_HANDLE = 1; 
        public const Int32 DEVICE_NOTIFY_WINDOW_HANDLE = 0; 
        public const Int32 WM_DEVICECHANGE = 0X219; 
        
        // from setupapi.h
        
        public const Int16 DIGCF_PRESENT = 0X2; 
        public const Int16 DIGCF_DEVICEINTERFACE = 0X10; 
        
        // There are two declarations for the DEV_BROADCAST_DEVICEINTERFACE class.
        
        // Use this in the call to RegisterDeviceNotification() and
        // in checking dbch_devicetype in a DEV_BROADCAST_HDR structure.
        
        [ StructLayout( LayoutKind.Sequential ) ]
        public class DEV_BROADCAST_DEVICEINTERFACE  
        { 
            public Int32 dbcc_size; 
            public Int32 dbcc_devicetype; 
            public Int32 dbcc_reserved; 
            public Guid dbcc_classguid; 
            public Int16 dbcc_name; 
        }         
        
        // Use this to read the dbcc_name String and classguid.
        
        [ StructLayout( LayoutKind.Sequential, CharSet=CharSet.Unicode ) ]
        public class DEV_BROADCAST_DEVICEINTERFACE_1  
        { 
            public Int32 dbcc_size; 
            public Int32 dbcc_devicetype; 
            public Int32 dbcc_reserved; 
            [ MarshalAs( UnmanagedType.ByValArray, ArraySubType=UnmanagedType.U1, SizeConst=16 ) ]public Byte[] dbcc_classguid; 
            [ MarshalAs( UnmanagedType.ByValArray, SizeConst=255 ) ]public char[] dbcc_name; 
        }      
        
        [ StructLayout( LayoutKind.Sequential ) ]
        public class DEV_BROADCAST_HANDLE  
        { 
            public Int32 dbch_size; 
            public Int32 dbch_devicetype; 
            public Int32 dbch_reserved; 
            public Int32 dbch_handle; 
            public Int32 dbch_hdevnotify; 
        }      
        
        [ StructLayout( LayoutKind.Sequential ) ]
        public class DEV_BROADCAST_HDR  
        { 
            public Int32 dbch_size; 
            public Int32 dbch_devicetype; 
            public Int32 dbch_reserved; 
        } 
        
        [ StructLayout( LayoutKind.Sequential ) ]
        public struct SP_DEVICE_INTERFACE_DATA 
        { 
            public Int32 cbSize; 
            public System.Guid InterfaceClassGuid; 
            public Int32 Flags; 
            public Int32 Reserved; 
        } 
        
        [ StructLayout( LayoutKind.Sequential ) ]
        public struct SP_DEVICE_INTERFACE_DETAIL_DATA 
        { 
            public Int32 cbSize; 
            public String DevicePath; 
        } 
         
        [ StructLayout( LayoutKind.Sequential ) ]
        public struct SP_DEVINFO_DATA 
        { 
            public Int32 cbSize; 
            public System.Guid ClassGuid; 
            public Int32 DevInst; 
            public Int32 Reserved; 
        } 
         
        [ DllImport( "user32.dll", CharSet=CharSet.Auto, SetLastError=true ) ]
        public static extern IntPtr RegisterDeviceNotification( IntPtr hRecipient, IntPtr NotificationFilter, Int32 Flags );
        
        [ DllImport( "setupapi.dll", SetLastError=true ) ]
        public static extern Int32 SetupDiCreateDeviceInfoList( ref System.Guid ClassGuid, Int32 hwndParent );        
        
        [ DllImport( "setupapi.dll", SetLastError=true ) ]
        public static extern Int32 SetupDiDestroyDeviceInfoList( IntPtr DeviceInfoSet );        
        
        [ DllImport( "setupapi.dll", SetLastError=true ) ]
        public static extern Boolean SetupDiEnumDeviceInterfaces( IntPtr DeviceInfoSet, Int32 DeviceInfoData, ref System.Guid InterfaceClassGuid, Int32 MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData );        
               
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SetupDiGetClassDevs(ref System.Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, Int32 Flags);        

        [ DllImport( "setupapi.dll", SetLastError=true, CharSet=CharSet.Auto ) ]
        public static extern Boolean SetupDiGetDeviceInterfaceDetail( IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, Int32 DeviceInterfaceDetailDataSize, ref Int32 RequiredSize, IntPtr DeviceInfoData );        
       
        [ DllImport( "user32.dll", SetLastError=true ) ]
        public static extern Boolean UnregisterDeviceNotification( IntPtr Handle );        
    } 
    
    
    
} 
