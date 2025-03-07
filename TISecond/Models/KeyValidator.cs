using System.Text;

namespace TISecond.Models;

public static class KeyValidator
{
    public static string ValidateKey(string key)
    {
        return new string(key.Where(c => c is '0' or '1').ToArray());
    }
}
