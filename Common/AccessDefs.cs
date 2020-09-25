using System;

namespace ManualMF
{
    public enum AccessState : byte { Pending = 0, Allowed = 1, Denied = 2 };
    internal enum AccessDeniedReason : byte { UnknownOrNotDenied=0, DeniedByOperator, DeniedByIP, DeniedByTimeOut, RecordDisappeared, InvalidToken }
    internal struct AccessStateAndReason
    {
        public AccessState State;
        public AccessDeniedReason Reason;
        public int? Token;
        public AccessStateAndReason(AccessState _State, AccessDeniedReason _Reason, int? _Token=null) { State = _State; Reason = _Reason; Token = _Token; }
    }
/*
    internal class EndpointAccessToken //This class is intended to distinguish between some overoalded methods
    {
        int? m_Token;
        public EndpointAccessToken(int? _Token) { m_Token = _Token; }
        public static implicit operator int?(EndpointAccessToken t) { return t.m_Token; }
    }
 */

}
