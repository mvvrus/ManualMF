using System;

namespace ManualMF
{
    internal enum AccessState : byte { Pending = 0, Allowed = 1, Denied = 2 };
    internal enum AccessDeniedReason : byte { UnknownOrNotDenied=0, DeniedByOperator, DeniedByIP, DeniedByTimeOut, RecordDisappeared }
    internal struct AccessStateAndReason
    {
        public AccessState State;
        public AccessDeniedReason Reason;
        public String Token;
        public AccessStateAndReason(AccessState _State, AccessDeniedReason _Reason, String _Token=null) { State = _State; Reason = _Reason; Token = _Token; }
    }

    internal class EndpointAccessToken //This class is intended to distinguish between some overoalded methods
    {
        String m_TokenString;
        public EndpointAccessToken(String _TokenString) { m_TokenString = _TokenString; }
        public static implicit operator String(EndpointAccessToken t) { return t.m_TokenString; }
    }

}
