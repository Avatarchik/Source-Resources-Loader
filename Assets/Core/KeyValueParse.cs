using System.Collections.Generic;
using System.Text.RegularExpressions;

public class KeyValueParse
{
    public static Dictionary<string, string> Load(string[] File)
    {
        Dictionary<string, string> Items = new Dictionary<string, string>();

        for (int i = 0; i < File.Length - 1; i++)
        {
            if ((uint)File[i].IndexOf("//") <= 2)
                continue;

            File[i] = File[i].Trim().Trim('\t', '{', '}')
                .Replace('"'.ToString(), "");

            if (!string.IsNullOrEmpty(File[i]))
            {
                File[i] = new Regex(@"\s+").Replace(File[i], @" ");

                string[] KeyValue = File[i].Split(' ');
                string Key = KeyValue[0].Normalize(), Value = string.Empty;

                if (KeyValue.Length <= 2)
                {
                    if (KeyValue.Length == 1)
                        System.Array.Resize(ref KeyValue, 2);

                    if (string.IsNullOrEmpty(KeyValue[1]))
                        KeyValue[1] = string.Empty;

                    Value = KeyValue[1].Normalize();
                }

                if (KeyValue.Length >= 3)
                {
                    int index = File[i].IndexOf(' ') + 1;
                    Value = File[i].Remove(0, index).Normalize();
                }

                if (!Items.ContainsKey(Key))
                    Items.Add(Key, Value);
            }
        }

        return Items;
    }
}
