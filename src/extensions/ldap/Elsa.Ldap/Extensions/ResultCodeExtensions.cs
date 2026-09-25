using System.DirectoryServices.Protocols;

namespace Elsa.Ldap.Extensions;

internal static class ResultCodeExtensions
{
    private static readonly ResultCode[] _resultCodesIndicatingSuccess =
    [
        ResultCode.Success,
        ResultCode.CompareFalse,
        ResultCode.CompareTrue,
        ResultCode.Referral,
        ResultCode.ReferralV2,
    ];

    public static bool IsSuccess(this ResultCode resultCode)
    {
        return _resultCodesIndicatingSuccess.Contains(resultCode);
    }

    public static bool IsError(this ResultCode resultCode)
    {
        return !resultCode.IsSuccess();
    }
}
