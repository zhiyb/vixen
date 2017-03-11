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
        
        public TCPLinkyData()
        {
            Address = new IPAddress(new byte[] { 0, 0, 0, 0 });
            Port = 12345;
            Stream = 0;
        }

        public override IModuleDataModel Clone()
        {
            TCPLinkyData result = new TCPLinkyData();
            result.Address = new IPAddress(Address.GetAddressBytes());
            result.Port = Port;
            result.Stream = Stream;
            return result;
        }
    }
}