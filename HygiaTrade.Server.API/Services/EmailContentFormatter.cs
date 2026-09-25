using System.Globalization;
using System.Net;

namespace HygiaTrade.API.Services;

public static class EmailContentFormatter
{
	public static string Encode(string value) =>
		WebUtility.HtmlEncode(value);

	public static string NormalizeSubject(string value)
	{
		string normalized = value
			.Replace('\r', ' ')
			.Replace('\n', ' ')
			.Trim();

		return string.IsNullOrWhiteSpace(normalized)
			? "Contact enquiry"
			: normalized;
	}

	public static string FormatMoney(decimal value) =>
		value.ToString(
			"C",
			CultureInfo.GetCultureInfo("bg-BG"));
}
