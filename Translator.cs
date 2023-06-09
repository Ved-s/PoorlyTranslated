﻿using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PoorlyTranslated
{
    public class Translator
    {
        HttpClient Client = new();
        Random Random = new();

        public readonly static string[] Languages = new[] { 
            "af", "ak", "am", "ar", "as", "ay", "az", "be", "bg", "bho", "bm", "bn", "bs", "ca", "ceb",
            "ckb", "co", "cs", "cy", "da", "de", "doi", "dv", "ee", "el", "en", "eo", "es", "et", "eu",
            "fa", "fi", "fr", "fy", "ga", "gd", "gl", "gn", "gom", "gu", "ha", "haw", "hi", "hmn", "hr",
            "ht", "hu", "hy", "id", "ig", "ilo", "is", "it", "iw", "ja", "jw", "ka", "kk", "km", "kn",
            "ko", "kri", "ku", "ky", "la", "lb", "lg", "ln", "lo", "lt", "lus", "lv", "mai", "mg", "mi",
            "mk", "ml", "mn", "mni-Mtei", "mr", "ms", "mt", "my", "ne", "nl", "no", "nso", "ny", "om",
            "or", "pa", "pl", "ps", "pt", "qu", "ro", "ru", "rw", "sa", "sd", "si", "sk", "sl", "sm", "sn",
            "so", "sq", "sr", "st", "su", "sv", "sw", "ta", "te", "tg", "th", "ti", "tk", "tl", "tr", "ts",
            "tt", "ug", "uk", "ur", "uz", "vi", "xh", "yi", "yo", "zh-CN", "zh-TW", "zu" 
        };

        public static long TranslationsDone;

        public const string AutoLang = "auto";

        public static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("Translator");

        public Translator() 
        {
            ServicePointManager.DefaultConnectionLimit = 1000;
        }

        public async Task<string?> PoorlyTranslate(string lang, string text, int times, CancellationToken? cancel = null)
        {
            string orig = text;
            for (int i = 0; i < times; i++)
            {
                cancel?.ThrowIfCancellationRequested();

                string? newText = await Translate(AutoLang, Languages[Random.Next(Languages.Length)], text, cancel);
                if (newText is null)
                    return null;

                text = newText;
            }

            cancel?.ThrowIfCancellationRequested();
            string? final = await Translate(AutoLang, lang, text, cancel);
            if (final is null)
                return null;

            return FixString(orig, final);
        }

        public async Task<string?> Translate(string srcLang, string dstLang, string text, CancellationToken? cancel = null)
        {
            if (!cancel.HasValue)
                Logger.LogWarning("cancel none");
            else if (!cancel.Value.CanBeCanceled)
                Logger.LogWarning("can't cancel");
            else if (cancel?.IsCancellationRequested is true)
                Logger.LogWarning("canceled???");

            string url = $"http://translate.googleapis.com/translate_a/single?client=gtx&sl={srcLang}&tl={dstLang}&dt=t&q={text}";
            HttpResponseMessage response = await Client.GetAsync(url, cancel ?? CancellationToken.None);

            if (!response.IsSuccessStatusCode)
                return null;

            cancel?.ThrowIfCancellationRequested();

            string json = await response.Content.ReadAsStringAsync();

            if (Json.Deserialize(json) is List<object> array0 && array0.FirstOrDefault() is List<object> array1)
            {
                StringBuilder builder = new();

                foreach (object obj in array1)
                {
                    if (obj is List<object> array2 && array2.FirstOrDefault() is string result)
                        builder.Append(result);
                }

                if (!cancel.HasValue)
                    Logger.LogWarning("cancel none");
                else if (!cancel.Value.CanBeCanceled)
                    Logger.LogWarning("can't cancel");
                else if (cancel?.IsCancellationRequested is true)
                    Logger.LogWarning("canceled???");

                Interlocked.Increment(ref TranslationsDone);
                return builder.ToString();
            }

            return $"JsonError({srcLang}, {dstLang}, {text}): {json}";
        }

        public string FixString(string old, string @new)
        {
            if (old.Contains("<") && old.Contains(">"))
            {
                StringBuilder builder = new();
                int pos = 0;
                Queue<string> keys = new();

                while (true)
                {
                    int startIndex = old.IndexOf('<', pos);
                    if (startIndex < 0)
                        break;
                    startIndex++;

                    int endIndex = old.IndexOf('>', startIndex);
                    if (endIndex < 0)
                        break;

                    keys.Enqueue(old.Substring(startIndex, endIndex - startIndex));
                    pos = endIndex + 1;
                }

                pos = 0;
                while (keys.Count > 0 && pos < @new.Length)
                {
                    string key = keys.Dequeue();

                    int startIndex = @new.IndexOf('<', pos);
                    if (startIndex < 0)
                        break;

                    int endIndex = @new.IndexOf('>', startIndex);
                    if (endIndex < 0)
                        break;

                    builder.Append(@new, pos, startIndex - pos);
                    builder.Append("<");
                    builder.Append(key);
                    builder.Append(">");
                    pos = endIndex + 1;
                }

                if (pos < @new.Length)
                    builder.Append(@new, pos, @new.Length - pos);

                @new = builder.ToString();
            }

            return Regex.Replace(@new, @"{\d(?!})|{(?!\d})(.*})?", "");
        }
    }
}
