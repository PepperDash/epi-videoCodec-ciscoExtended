using System;
using System.Text;
using Newtonsoft.Json.Linq;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
  #region

  public static class ExtensionsMethods
  {
    public static string EncodeBase64(this string plainText)
    {
      var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
      return Convert.ToBase64String(plainTextBytes);
    }

    public static string DecodeBase64(this string encodedText)
    {
      var encodedTextBytes = Encoding.UTF8.GetBytes(encodedText);
      return Convert.ToString(encodedTextBytes);
    }

    public static JToken GetValueProperty(this JObject jObject, string path)
    {
      JToken outToken;

      if (!jObject.TryGetValue(path, out outToken))
        return null;
      var outputValue = outToken.SelectToken("Value");
      return outputValue;
    }
  }

  #endregion
}
