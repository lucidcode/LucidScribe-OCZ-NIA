using Microsoft.Win32.SafeHandles; 
using System.Runtime.InteropServices; 

///  <summary>
///  API declarations relating to file I/O.
///  </summary>

using System;

namespace GenericHid
{
    public sealed class FileIO  
    {         
        public const Int32 FILE_FLAG_OVERLAPPED = 0X40000000; 
        public const Int16 FILE_SHARE_READ = 0X1; 
        public const Int16 FILE_SHARE_WRITE = 0X2; 
        public const Int32 GENERIC_READ = unchecked (( int )0X80000000); 
        public const Int32 GENERIC_WRITE = 0X40000000; 
        public const Int32 INVALID_HANDLE_VALUE = -1; 
        public const Int16 OPEN_EXISTING = 3; 
        public const Int32 WAIT_TIMEOUT = 0X102; 
        public const Int16 WAIT_OBJECT_0 = 0; 
        
        [ StructLayout( LayoutKind.Sequential ) ]
        public class OVERLAPPED 
        { 
            public Int32 Internal; 
            public Int32 InternalHigh; 
            public Int32 Offset; 
            public Int32 OffsetHigh; 
            public SafeWaitHandle hEvent; 
        }   
       
        [ StructLayout( LayoutKind.Sequential ) ]
        public class SECURITY_ATTRIBUTES  
        { 
            public Int32 nLength; 
            public Int32 lpSecurityDescriptor; 
            public Int32 bInheritHandle; 
        }    
        
        [ DllImport( "kernel32.dll", SetLastError=true ) ]
        public static extern Int32 CancelIo( SafeFileHandle hFile );        
        
        [ DllImport( "kernel32.dll", CharSet=CharSet.Auto, SetLastError=true ) ]
        public static extern SafeWaitHandle CreateEvent( SECURITY_ATTRIBUTES SecurityAttributes, Int32 bManualReset, Int32 bInitialState, String lpName );        
       
        [ DllImport( "kernel32.dll", CharSet=CharSet.Auto, SetLastError=true ) ]
        public static extern SafeFileHandle CreateFile( String lpFileName, Int32 dwDesiredAccess, Int32 dwShareMode, SECURITY_ATTRIBUTES lpSecurityAttributes, Int32 dwCreationDisposition, Int32 dwFlagsAndAttributes, Int32 hTemplateFile );        
       
        [ DllImport( "kernel32.dll", SetLastError=true ) ]
        public static extern Int32 ReadFile( SafeFileHandle hFile, Byte[] lpBuffer, Int32 nNumberOfBytesToRead, ref Int32 lpNumberOfBytesRead, OVERLAPPED lpOverlapped );        
       
        [ DllImport( "kernel32.dll", SetLastError=true ) ]
        public static extern Int32 WaitForSingleObject( SafeWaitHandle hHandle, Int32 dwMilliseconds );        
       
        [ DllImport( "kernel32.dll", SetLastError=true ) ]
        public static extern Boolean WriteFile(SafeFileHandle hFile, Byte[] lpBuffer, Int32 nNumberOfBytesToWrite, ref Int32 lpNumberOfBytesWritten, OVERLAPPED lpOverlapped);        
    }     
} 
