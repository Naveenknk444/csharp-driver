//
//      Copyright (C) 2012 DataStax Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//
﻿using System.Collections.Generic;
namespace Cassandra
{
    internal class BatchRequest : IRequest
    {
        public const byte OpCode = 0x0D;

        readonly int _streamId;
        readonly ConsistencyLevel _consistency;
        private readonly byte _flags = 0x00;
        private readonly ICollection<IBatchableRequest> _requests;
        private readonly BatchType _type;

        public enum BatchType { Logged=0 , Unlogged=1, Counter = 2 }

        public BatchRequest(int streamId, BatchType type, ICollection<IBatchableRequest> requests, ConsistencyLevel consistency, bool tracingEnabled)
        {
            this._type = type;
            this._requests = requests;
            this._streamId = streamId;
            this._consistency = consistency;
            if (tracingEnabled)
                this._flags = 0x02;
        }

        public RequestFrame GetFrame()
        {
            var wb = new BEBinaryWriter();
            wb.WriteFrameHeader(RequestFrame.ProtocolRequestVersionByte, _flags, (byte)_streamId, OpCode);
            wb.WriteByte((byte)_flags);
            wb.WriteInt16((short)_requests.Count);
            foreach (var br in _requests)
                br.WriteToBatch(wb);
            wb.WriteInt16((short)_consistency);
            return wb.GetFrame();
        }
    }
}
