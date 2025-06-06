using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GDMiniJSON
{
    // Token: 0x02000445 RID: 1093
    public static class Json
    {
        // Token: 0x0600237C RID: 9084 RVA: 0x00073124 File Offset: 0x00071324
        public static object Deserialize(string json)
        {
            if (json == null)
            {
                return null;
            }
            return Json.Parser.Parse(json);
        }

        // Token: 0x0600237D RID: 9085 RVA: 0x00073131 File Offset: 0x00071331
        public static object DeserializePartially(string json, string upToSection)
        {
            if (json == null)
            {
                return null;
            }
            return Json.Parser.ParsePartially(json, upToSection);
        }

        // Token: 0x0600237E RID: 9086 RVA: 0x0007313F File Offset: 0x0007133F
        public static string Serialize(object obj)
        {
            return Json.Serializer.Serialize(obj);
        }

        // Token: 0x02000560 RID: 1376
        private sealed class Parser : IDisposable
        {
            // Token: 0x060029F3 RID: 10739 RVA: 0x0009073B File Offset: 0x0008E93B
            private Parser(string jsonString)
            {
                this.json = new StringReader(jsonString);
            }

            // Token: 0x060029F4 RID: 10740 RVA: 0x00090750 File Offset: 0x0008E950
            public static object Parse(string jsonString)
            {
                object result;
                using (Json.Parser parser = new Json.Parser(jsonString))
                {
                    result = parser.ParseValue();
                }
                return result;
            }

            // Token: 0x060029F5 RID: 10741 RVA: 0x00090788 File Offset: 0x0008E988
            public static object ParsePartially(string jsonString, string upToSection)
            {
                object result;
                using (Json.Parser parser = new Json.Parser(jsonString))
                {
                    parser.endSection = upToSection;
                    result = parser.ParseValue();
                }
                return result;
            }

            // Token: 0x060029F6 RID: 10742 RVA: 0x000907C8 File Offset: 0x0008E9C8
            public void Dispose()
            {
                this.json.Dispose();
                this.json = null;
            }

            // Token: 0x060029F7 RID: 10743 RVA: 0x000907DC File Offset: 0x0008E9DC
            private Dictionary<string, object> ParseObject()
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                this.json.Read();
                for (; ; )
                {
                    Json.Parser.TOKEN nextToken = this.NextToken;
                    if (nextToken == Json.Parser.TOKEN.NONE)
                    {
                        break;
                    }
                    if (nextToken == Json.Parser.TOKEN.CURLY_CLOSE)
                    {
                        return dictionary;
                    }
                    if (nextToken != Json.Parser.TOKEN.COMMA)
                    {
                        string text = this.ParseString();
                        if (text == null)
                        {
                            goto Block_4;
                        }
                        if (this.NextToken != Json.Parser.TOKEN.COLON)
                        {
                            goto Block_5;
                        }
                        if (this.endSection != null && string.Equals(text, this.endSection))
                        {
                            return dictionary;
                        }
                        this.json.Read();
                        dictionary[text] = this.ParseValue();
                    }
                }
                return null;
            Block_4:
                return null;
            Block_5:
                return null;
            }

            // Token: 0x060029F8 RID: 10744 RVA: 0x0009085C File Offset: 0x0008EA5C
            private List<object> ParseArray()
            {
                List<object> list = new List<object>();
                this.json.Read();
                bool flag = true;
                while (flag)
                {
                    Json.Parser.TOKEN nextToken = this.NextToken;
                    if (nextToken == Json.Parser.TOKEN.NONE)
                    {
                        return null;
                    }
                    if (nextToken != Json.Parser.TOKEN.SQUARED_CLOSE)
                    {
                        if (nextToken != Json.Parser.TOKEN.COMMA)
                        {
                            object item = this.ParseByToken(nextToken);
                            list.Add(item);
                        }
                    }
                    else
                    {
                        flag = false;
                    }
                }
                return list;
            }

            // Token: 0x060029F9 RID: 10745 RVA: 0x000908AC File Offset: 0x0008EAAC
            private object ParseValue()
            {
                Json.Parser.TOKEN nextToken = this.NextToken;
                return this.ParseByToken(nextToken);
            }

            // Token: 0x060029FA RID: 10746 RVA: 0x000908C8 File Offset: 0x0008EAC8
            private object ParseByToken(Json.Parser.TOKEN token)
            {
                switch (token)
                {
                    case Json.Parser.TOKEN.CURLY_OPEN:
                        return this.ParseObject();
                    case Json.Parser.TOKEN.SQUARED_OPEN:
                        return this.ParseArray();
                    case Json.Parser.TOKEN.STRING:
                        return this.ParseString();
                    case Json.Parser.TOKEN.NUMBER:
                        return this.ParseNumber();
                    case Json.Parser.TOKEN.TRUE:
                        return true;
                    case Json.Parser.TOKEN.FALSE:
                        return false;
                    case Json.Parser.TOKEN.NULL:
                        return null;
                }
                return null;
            }

            // Token: 0x060029FB RID: 10747 RVA: 0x00090938 File Offset: 0x0008EB38
            private string ParseString()
            {
                StringBuilder stringBuilder = new StringBuilder();
                this.json.Read();
                bool flag = true;
                while (flag)
                {
                    if (this.json.Peek() == -1)
                    {
                        break;
                    }
                    char nextChar = this.NextChar;
                    if (nextChar != '"')
                    {
                        if (nextChar != '\\')
                        {
                            stringBuilder.Append(nextChar);
                        }
                        else if (this.json.Peek() == -1)
                        {
                            flag = false;
                        }
                        else
                        {
                            nextChar = this.NextChar;
                            if (nextChar <= '\\')
                            {
                                if (nextChar == '"' || nextChar == '/' || nextChar == '\\')
                                {
                                    stringBuilder.Append(nextChar);
                                }
                            }
                            else if (nextChar <= 'f')
                            {
                                if (nextChar != 'b')
                                {
                                    if (nextChar == 'f')
                                    {
                                        stringBuilder.Append('\f');
                                    }
                                }
                                else
                                {
                                    stringBuilder.Append('\b');
                                }
                            }
                            else if (nextChar != 'n')
                            {
                                switch (nextChar)
                                {
                                    case 'r':
                                        stringBuilder.Append('\r');
                                        break;
                                    case 't':
                                        stringBuilder.Append('\t');
                                        break;
                                    case 'u':
                                        {
                                            StringBuilder stringBuilder2 = new StringBuilder();
                                            for (int i = 0; i < 4; i++)
                                            {
                                                stringBuilder2.Append(this.NextChar);
                                            }
                                            stringBuilder.Append((char)Convert.ToInt32(stringBuilder2.ToString(), 16));
                                            break;
                                        }
                                }
                            }
                            else
                            {
                                stringBuilder.Append('\n');
                            }
                        }
                    }
                    else
                    {
                        flag = false;
                    }
                }
                return stringBuilder.ToString();
            }

            // Token: 0x060029FC RID: 10748 RVA: 0x00090A8C File Offset: 0x0008EC8C
            private object ParseNumber()
            {
                string nextWord = this.NextWord;
                if (nextWord.IndexOf('.') == -1)
                {
                    int num;
                    int.TryParse(nextWord, out num);
                    return num;
                }
                float num2;
                float.TryParse(nextWord, out num2);
                return num2;
            }

            // Token: 0x060029FD RID: 10749 RVA: 0x00090ACA File Offset: 0x0008ECCA
            private void EatWhitespace()
            {
                while (" \t\n\r".IndexOf(this.PeekChar) != -1)
                {
                    this.json.Read();
                    if (this.json.Peek() == -1)
                    {
                        break;
                    }
                }
            }

            // Token: 0x170017E1 RID: 6113
            // (get) Token: 0x060029FE RID: 10750 RVA: 0x00090AFB File Offset: 0x0008ECFB
            private char PeekChar
            {
                get
                {
                    return Convert.ToChar(this.json.Peek());
                }
            }

            // Token: 0x170017E2 RID: 6114
            // (get) Token: 0x060029FF RID: 10751 RVA: 0x00090B0D File Offset: 0x0008ED0D
            private char NextChar
            {
                get
                {
                    return Convert.ToChar(this.json.Read());
                }
            }

            // Token: 0x170017E3 RID: 6115
            // (get) Token: 0x06002A00 RID: 10752 RVA: 0x00090B20 File Offset: 0x0008ED20
            private string NextWord
            {
                get
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    while (" \t\n\r{}[],:\"".IndexOf(this.PeekChar) == -1)
                    {
                        stringBuilder.Append(this.NextChar);
                        if (this.json.Peek() == -1)
                        {
                            break;
                        }
                    }
                    return stringBuilder.ToString();
                }
            }

            // Token: 0x170017E4 RID: 6116
            // (get) Token: 0x06002A01 RID: 10753 RVA: 0x00090B6C File Offset: 0x0008ED6C
            private Json.Parser.TOKEN NextToken
            {
                get
                {
                    this.EatWhitespace();
                    if (this.json.Peek() == -1)
                    {
                        return Json.Parser.TOKEN.NONE;
                    }
                    char peekChar = this.PeekChar;
                    if (peekChar <= '[')
                    {
                        switch (peekChar)
                        {
                            case '"':
                                return Json.Parser.TOKEN.STRING;
                            case '#':
                            case '$':
                            case '%':
                            case '&':
                            case '\'':
                            case '(':
                            case ')':
                            case '*':
                            case '+':
                            case '.':
                            case '/':
                                break;
                            case ',':
                                this.json.Read();
                                return Json.Parser.TOKEN.COMMA;
                            case '-':
                            case '0':
                            case '1':
                            case '2':
                            case '3':
                            case '4':
                            case '5':
                            case '6':
                            case '7':
                            case '8':
                            case '9':
                                return Json.Parser.TOKEN.NUMBER;
                            case ':':
                                return Json.Parser.TOKEN.COLON;
                            default:
                                if (peekChar == '[')
                                {
                                    return Json.Parser.TOKEN.SQUARED_OPEN;
                                }
                                break;
                        }
                    }
                    else
                    {
                        if (peekChar == ']')
                        {
                            this.json.Read();
                            return Json.Parser.TOKEN.SQUARED_CLOSE;
                        }
                        if (peekChar == '{')
                        {
                            return Json.Parser.TOKEN.CURLY_OPEN;
                        }
                        if (peekChar == '}')
                        {
                            this.json.Read();
                            return Json.Parser.TOKEN.CURLY_CLOSE;
                        }
                    }
                    string nextWord = this.NextWord;
                    if (nextWord == "false")
                    {
                        return Json.Parser.TOKEN.FALSE;
                    }
                    if (nextWord == "true")
                    {
                        return Json.Parser.TOKEN.TRUE;
                    }
                    if (!(nextWord == "null"))
                    {
                        return Json.Parser.TOKEN.NONE;
                    }
                    return Json.Parser.TOKEN.NULL;
                }
            }

            // Token: 0x04002B69 RID: 11113
            private const string WHITE_SPACE = " \t\n\r";

            // Token: 0x04002B6A RID: 11114
            private const string WORD_BREAK = " \t\n\r{}[],:\"";

            // Token: 0x04002B6B RID: 11115
            private StringReader json;

            // Token: 0x04002B6C RID: 11116
            private string endSection;

            // Token: 0x02000681 RID: 1665
            private enum TOKEN
            {
                // Token: 0x04002D6B RID: 11627
                NONE,
                // Token: 0x04002D6C RID: 11628
                CURLY_OPEN,
                // Token: 0x04002D6D RID: 11629
                CURLY_CLOSE,
                // Token: 0x04002D6E RID: 11630
                SQUARED_OPEN,
                // Token: 0x04002D6F RID: 11631
                SQUARED_CLOSE,
                // Token: 0x04002D70 RID: 11632
                COLON,
                // Token: 0x04002D71 RID: 11633
                COMMA,
                // Token: 0x04002D72 RID: 11634
                STRING,
                // Token: 0x04002D73 RID: 11635
                NUMBER,
                // Token: 0x04002D74 RID: 11636
                TRUE,
                // Token: 0x04002D75 RID: 11637
                FALSE,
                // Token: 0x04002D76 RID: 11638
                NULL
            }
        }

        // Token: 0x02000561 RID: 1377
        private sealed class Serializer
        {
            // Token: 0x06002A02 RID: 10754 RVA: 0x00090C8E File Offset: 0x0008EE8E
            private Serializer()
            {
                this.builder = new StringBuilder();
            }

            // Token: 0x06002A03 RID: 10755 RVA: 0x00090CA1 File Offset: 0x0008EEA1
            public static string Serialize(object obj)
            {
                Json.Serializer serializer = new Json.Serializer();
                serializer.SerializeValue(obj);
                return serializer.builder.ToString();
            }

            // Token: 0x06002A04 RID: 10756 RVA: 0x00090CBC File Offset: 0x0008EEBC
            private void SerializeValue(object value)
            {
                if (value == null)
                {
                    this.builder.Append("null");
                    return;
                }
                string str;
                if ((str = (value as string)) != null)
                {
                    this.SerializeString(str);
                    return;
                }
                if (value is bool)
                {
                    this.builder.Append(value.ToString().ToLower());
                    return;
                }
                IList anArray;
                if ((anArray = (value as IList)) != null)
                {
                    this.SerializeArray(anArray);
                    return;
                }
                IDictionary obj;
                if ((obj = (value as IDictionary)) != null)
                {
                    this.SerializeObject(obj);
                    return;
                }
                if (value is char)
                {
                    this.SerializeString(value.ToString());
                    return;
                }
                this.SerializeOther(value);
            }

            // Token: 0x06002A05 RID: 10757 RVA: 0x00090D50 File Offset: 0x0008EF50
            private void SerializeObject(IDictionary obj)
            {
                bool flag = true;
                this.builder.Append("{\n");
                foreach (object obj2 in obj.Keys)
                {
                    if (!flag)
                    {
                        this.builder.Append(",\n");
                    }
                    this.SerializeString(obj2.ToString());
                    this.builder.Append(':');
                    this.SerializeValue(obj[obj2]);
                    flag = false;
                }
                this.builder.Append("\n}");
            }

            // Token: 0x06002A06 RID: 10758 RVA: 0x00090E00 File Offset: 0x0008F000
            private void SerializeArray(IList anArray)
            {
                this.builder.Append('[');
                bool flag = true;
                foreach (object value in anArray)
                {
                    if (!flag)
                    {
                        this.builder.Append(',');
                    }
                    this.SerializeValue(value);
                    flag = false;
                }
                this.builder.Append(']');
            }

            // Token: 0x06002A07 RID: 10759 RVA: 0x00090E80 File Offset: 0x0008F080
            private void SerializeString(string str)
            {
                this.builder.Append('"');
                char[] array = str.ToCharArray();
                int i = 0;
                while (i < array.Length)
                {
                    char c = array[i];
                    switch (c)
                    {
                        case '\b':
                            this.builder.Append("\\b");
                            break;
                        case '\t':
                            this.builder.Append("\\t");
                            break;
                        case '\n':
                            this.builder.Append("\\n");
                            break;
                        case '\v':
                            goto IL_DD;
                        case '\f':
                            this.builder.Append("\\f");
                            break;
                        case '\r':
                            this.builder.Append("\\r");
                            break;
                        default:
                            if (c != '"')
                            {
                                if (c != '\\')
                                {
                                    goto IL_DD;
                                }
                                this.builder.Append("\\\\");
                            }
                            else
                            {
                                this.builder.Append("\\\"");
                            }
                            break;
                    }
                IL_123:
                    i++;
                    continue;
                IL_DD:
                    int num = Convert.ToInt32(c);
                    if (num >= 32 && num <= 126)
                    {
                        this.builder.Append(c);
                        goto IL_123;
                    }
                    this.builder.Append("\\u" + Convert.ToString(num, 16).PadLeft(4, '0'));
                    goto IL_123;
                }
                this.builder.Append('"');
            }

            // Token: 0x06002A08 RID: 10760 RVA: 0x00090FCC File Offset: 0x0008F1CC
            private void SerializeOther(object value)
            {
                if (value is float || value is int || value is uint || value is long || value is double || value is sbyte || value is byte || value is short || value is ushort || value is ulong || value is decimal)
                {
                    this.builder.Append(value.ToString());
                    return;
                }
                this.SerializeString(value.ToString());
            }

            // Token: 0x04002B6D RID: 11117
            private StringBuilder builder;
        }
    }
}
