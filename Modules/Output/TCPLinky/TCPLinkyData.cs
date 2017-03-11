using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Net;
using Vixen.Module;

namespace VixenModules.Output.TCPLinky
{
    [DataContract]
    public class TCPLinkyData : ModuleDataModelBase
    {
        [DataMember]
        public IPAddress Address { get; set; }

        [DataMember]
        public int Port { get; set; }

        [DataMember]
        public int Stream { get; set; }

        [DataMember]
        public string File { get; set; }

        public TCPLinkyData()
        {
            Address = new IPAddress(new byte[] { 0, 0, 0, 0 });
            Port = 6000;
            Stream = 0;
            var path = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Vixen");
            File = path + "\\linky.bin";
        }

        public override IModuleDataModel Clone()
        {
            TCPLinkyData result = new TCPLinkyData();
            result.Address = new IPAddress(Address.GetAddressBytes());
            result.Port = Port;
            result.Stream = Stream;
            result.File = File;
            return result;
        }
    }
}