using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocTree.Models;

namespace DocTree.Services.Settings
{
    public static class SettingsRootWriter
    {
        private static readonly JsonSerializerOptions RootOptions = CreateRootOptions();

        public static bool AddRoot(string settingsPath, RootFolder root)
        {
            var loaded = SettingsLoader.Load(settingsPath);
            if (loaded.Settings.Roots.Any(r => SamePath(r.Path, root.Path)))
            {
                return false;
            }

            var json = File.ReadAllText(settingsPath, Encoding.UTF8);
            var rootsArray = FindPropertyArray(json, "roots");
            if (rootsArray is null)
            {
                throw new InvalidDataException("project-settings.jsonc に \"roots\" 配列が見つかりません。");
            }

            var fragment = BuildRootFragment(root);
            var insertion = loaded.Settings.Roots.Count == 0
                ? "\r\n" + Indent(fragment, "    ") + "\r\n  "
                : ",\r\n" + Indent(fragment, "    ") + "\r\n  ";

            var updated = json.Insert(rootsArray.Value.CloseBracketIndex, insertion);
            File.WriteAllText(settingsPath, updated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return true;
        }

        public static bool RemoveRoot(string settingsPath, string rootPath)
        {
            var loaded = SettingsLoader.Load(settingsPath);
            if (!loaded.Settings.Roots.Any(r => SamePath(r.Path, rootPath)))
            {
                return false;
            }

            var json = File.ReadAllText(settingsPath, Encoding.UTF8);
            var rootsArray = FindPropertyArray(json, "roots");
            if (rootsArray is null)
            {
                throw new InvalidDataException("project-settings.jsonc に \"roots\" 配列が見つかりません。");
            }

            var range = FindRootElementRange(json, rootsArray.Value, rootPath);
            if (range is null)
            {
                return false;
            }

            var updated = json.Remove(range.Value.Start, range.Value.EndExclusive - range.Value.Start);
            File.WriteAllText(settingsPath, updated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return true;
        }

        private static bool SamePath(string left, string right)
        {
            var l = NormalizePath(left);
            var r = NormalizePath(right);
            return string.Equals(l, r, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static string BuildRootFragment(RootFolder root)
        {
            return "{\r\n" +
                "  \"name\": " + JsonSerializer.Serialize(root.Name) + ",\r\n" +
                "  \"path\": " + JsonSerializer.Serialize(root.Path) + ",\r\n" +
                "  \"readOnly\": \"inherit\"\r\n" +
                "}";
        }

        private static JsonSerializerOptions CreateRootOptions()
        {
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
            return options;
        }

        private static string Indent(string text, string indent)
        {
            return indent + text.Replace("\r\n", "\r\n" + indent);
        }

        private static ArrayLocation? FindPropertyArray(string text, string propertyName)
        {
            for (int i = 0; i < text.Length;)
            {
                i = SkipTrivia(text, i);
                if (i >= text.Length) break;

                if (text[i] == '"')
                {
                    var stringEnd = ReadString(text, i);
                    var name = JsonSerializer.Deserialize<string>(text.Substring(i, stringEnd - i + 1));
                    i = SkipTrivia(text, stringEnd + 1);
                    if (i < text.Length && text[i] == ':')
                    {
                        i = SkipTrivia(text, i + 1);
                        if (string.Equals(name, propertyName, StringComparison.Ordinal) &&
                            i < text.Length && text[i] == '[')
                        {
                            return new ArrayLocation(i, FindMatchingArrayClose(text, i));
                        }
                    }
                    continue;
                }

                i++;
            }

            return null;
        }

        private static TextRange? FindRootElementRange(string text, ArrayLocation rootsArray, string rootPath)
        {
            for (int i = rootsArray.OpenBracketIndex + 1; i < rootsArray.CloseBracketIndex;)
            {
                i = SkipTrivia(text, i);
                if (i >= rootsArray.CloseBracketIndex) break;

                if (text[i] == ',')
                {
                    i++;
                    continue;
                }

                if (text[i] != '{')
                {
                    throw new InvalidDataException("project-settings.jsonc の \"roots\" 配列にオブジェクト以外の値があります。");
                }

                var valueStart = i;
                var valueEnd = FindMatchingObjectClose(text, i) + 1;
                var root = JsonSerializer.Deserialize<RootFolder>(
                    text.Substring(valueStart, valueEnd - valueStart),
                    RootOptions);

                if (root is not null && SamePath(root.Path, rootPath))
                {
                    return ExpandElementRemovalRange(text, rootsArray, valueStart, valueEnd);
                }

                i = valueEnd;
            }

            return null;
        }

        private static TextRange ExpandElementRemovalRange(string text, ArrayLocation rootsArray, int valueStart, int valueEnd)
        {
            var afterValue = SkipTrivia(text, valueEnd);
            if (afterValue < rootsArray.CloseBracketIndex && text[afterValue] == ',')
            {
                return new TextRange(valueStart, afterValue + 1);
            }

            var beforeValue = FindPreviousTopLevelComma(text, rootsArray.OpenBracketIndex + 1, valueStart);
            return beforeValue is int commaIndex
                ? new TextRange(commaIndex, valueEnd)
                : new TextRange(valueStart, valueEnd);
        }

        private static int? FindPreviousTopLevelComma(string text, int start, int end)
        {
            int? result = null;
            var objectDepth = 0;
            var arrayDepth = 0;

            for (int i = start; i < end; i++)
            {
                i = SkipTrivia(text, i);
                if (i >= end) break;

                if (text[i] == '"')
                {
                    i = ReadString(text, i);
                    continue;
                }

                switch (text[i])
                {
                    case '{':
                        objectDepth++;
                        break;
                    case '}':
                        objectDepth--;
                        break;
                    case '[':
                        arrayDepth++;
                        break;
                    case ']':
                        arrayDepth--;
                        break;
                    case ',' when objectDepth == 0 && arrayDepth == 0:
                        result = i;
                        break;
                }
            }

            return result;
        }

        private static int SkipTrivia(string text, int index)
        {
            while (index < text.Length)
            {
                if (char.IsWhiteSpace(text[index]))
                {
                    index++;
                    continue;
                }

                if (index + 1 < text.Length && text[index] == '/' && text[index + 1] == '/')
                {
                    index += 2;
                    while (index < text.Length && text[index] is not '\r' and not '\n') index++;
                    continue;
                }

                if (index + 1 < text.Length && text[index] == '/' && text[index + 1] == '*')
                {
                    index += 2;
                    while (index + 1 < text.Length && (text[index] != '*' || text[index + 1] != '/')) index++;
                    if (index + 1 >= text.Length) throw new InvalidDataException("project-settings.jsonc のブロックコメントが閉じていません。");
                    index += 2;
                    continue;
                }

                break;
            }

            return index;
        }

        private static int ReadString(string text, int quoteIndex)
        {
            for (int i = quoteIndex + 1; i < text.Length; i++)
            {
                if (text[i] == '\\')
                {
                    i++;
                    continue;
                }
                if (text[i] == '"') return i;
            }

            throw new InvalidDataException("project-settings.jsonc の文字列が閉じていません。");
        }

        private static int FindMatchingArrayClose(string text, int openBracketIndex)
        {
            var depth = 0;
            for (int i = openBracketIndex; i < text.Length; i++)
            {
                i = SkipTrivia(text, i);
                if (i >= text.Length) break;

                if (text[i] == '"')
                {
                    i = ReadString(text, i);
                    continue;
                }

                if (text[i] == '[')
                {
                    depth++;
                    continue;
                }

                if (text[i] == ']')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }

            throw new InvalidDataException("project-settings.jsonc の \"roots\" 配列が閉じていません。");
        }

        private static int FindMatchingObjectClose(string text, int openBraceIndex)
        {
            var depth = 0;
            for (int i = openBraceIndex; i < text.Length; i++)
            {
                i = SkipTrivia(text, i);
                if (i >= text.Length) break;

                if (text[i] == '"')
                {
                    i = ReadString(text, i);
                    continue;
                }

                if (text[i] == '{')
                {
                    depth++;
                    continue;
                }

                if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }

            throw new InvalidDataException("project-settings.jsonc のルートフォルダ定義が閉じていません。");
        }

        private readonly record struct ArrayLocation(int OpenBracketIndex, int CloseBracketIndex);
        private readonly record struct TextRange(int Start, int EndExclusive);
    }
}
