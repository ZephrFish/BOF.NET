﻿
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace BOFNET {
    public class BeaconConsoleWriter : BeaconOutputWriter {

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void BeaconConsoleWriterDelegate([MarshalAs(UnmanagedType.LPArray)] byte[] data, int len);

        private class BeaconStream : MemoryStream {

            object syncLock = new object();

            public uint FlushTrigger { get; set; } = 4096;

            Thread ownerThread;
            BeaconConsoleWriterDelegate beaconConsoleWriter;

            public BeaconStream(BeaconConsoleWriterDelegate beaconConsoleWriter, Thread ownerThread) {
                this.beaconConsoleWriter = beaconConsoleWriter;
                this.ownerThread = ownerThread;
            }

            public override void Write(byte[] buffer, int offset, int count) {
                lock (syncLock) {
                    base.Write(buffer, offset, count);
                    if (Position > FlushTrigger) {
                        Flush();
                    }
                }
            }

            public override void Flush() {
                base.Flush();

                if (Position > 0 && beaconConsoleWriter != null && ownerThread == Thread.CurrentThread) {
                    byte[] data = new byte[Position];
                    Seek(0, SeekOrigin.Begin);
                    Read(data, 0, data.Length);
                    beaconConsoleWriter(data, data.Length);
                    Seek(0, SeekOrigin.Begin);
                }                            
            }

            public override void Close() {
                beaconConsoleWriter = null;
            }
        }

        public BeaconConsoleWriter(BeaconConsoleWriterDelegate beaconConsoleWriter) : base(new BeaconStream(beaconConsoleWriter, Thread.CurrentThread)){
        }
 
        protected override void Dispose(bool disposing) {
            Flush();
            base.Dispose(disposing);
        }
    }
}
