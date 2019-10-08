#if !FINAL
#define VALIDATE_MAIN_THREAD_ENABLED
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gears
{
    public enum Categories
    {
        None,
        NetTraffic,
        SteamAPI,
        NetworkInterfaceLayer,
        ArenaEvents,
        CustomLobbyEvents,
        Initialization,
        FMOD,
        NumCategories
    }

    public enum Importance
    {
        VERBOSE = 0,  // Use for INFO messages that print too often (use discretion)
        INFO = 1,     // Default level of logging
        WARNING = 2,
        ERROR = 3
    }

    public class DebugTools
    {

		private static string _logFileName;

        public enum Categories
        {
            None,
            NetTraffic,
            SteamAPI,
            NetworkInterfaceLayer,
            ArenaEvents,
            CustomLobbyEvents,
            Initialization,
            FMOD,
            NumCategories
        }

        public enum Importance
        {
            VERBOSE = 0,  // Use for INFO messages that print too often (use discretion)
            INFO = 1,     // Default level of logging
            WARNING = 2,
            ERROR = 3
        }

        #region LogOut Type

        public struct LogOut
        {
            private readonly DebugTools.Categories _cat;
            private readonly DebugTools.Importance _imp;
            private readonly bool _printCallstack;

            public LogOut(DebugTools.Categories cat, DebugTools.Importance imp, bool printCallstack)
            {
                _cat = cat;
                _imp = imp;
                _printCallstack = printCallstack;
            }

            #region Emit

            public void Emit(string str)
            {
                if (DebugTools._sb == null)
                    return;
                var sb = DebugTools._sb;

                if (_imp >= Importance.WARNING)
                {
                    bool isSingleLine = !str.Contains('\n');
                    if (isSingleLine)
                    {
                        sb.Append(_imp);
                        sb.Append(": ");
                    }
                    else
                    {
                        sb.Append(_imp);
                        sb.Append(":\n");
                    }
                }
                else
                {
                    //Console.WriteLine(formatStr, a, b);
                }

                sb.Append(str);

                OutputToConsole(sb);

                sb.Clear();

                if (this._printCallstack)
                    Console.WriteLine(Environment.StackTrace);

                if (!_active)
                    return;

                if (_htmlWriter == null)
                    return;

                // jcf: generates garbage! should be converted to take a StringBuilder, probably
                OutputHtml(str, _cat, _printCallstack, _imp);
            }

            public void Emit<A>(string formatStr, A a)
#if WINRT
                where A : IComparable
#else
                where A : IConvertible
#endif
            {
                if (DebugTools._sb == null)
                    return;
                var sb = DebugTools._sb;

                if (_imp >= Importance.WARNING)
                {
                    bool isSingleLine = !formatStr.Contains('\n');
                    if (isSingleLine)
                    {
                        sb.Append(_imp);
                        sb.Append(": ");
                    }
                    else
                    {
                        sb.Append(_imp);
                        sb.Append(":\n");
                    }
                }
                else
                {
                    //Console.WriteLine(formatStr, a, b);
                }

                sb.AppendFormat(formatStr, a);

                OutputToConsole(sb);

                sb.Clear();

                if (_printCallstack)
                    Console.WriteLine(Environment.StackTrace);

                if (!_active)
                    return;

                if (_htmlWriter == null)
                    return;

                // jcf: generates garbage! should be converted to take a StringBuilder, probably
                OutputHtml(string.Format(formatStr, a), _cat, _printCallstack, _imp);
            }

            public void Emit<A, B>(string formatStr, A a, B b)
#if WINRT
                where A : IComparable
                where B : IComparable
#else
                where A : IConvertible
                where B : IConvertible
#endif   
            {
                if (DebugTools._sb == null)
                    return;
                var sb = DebugTools._sb;                

                if (_imp >= Importance.WARNING)
                {
                    bool isSingleLine = !formatStr.Contains('\n');
                    if (isSingleLine)
                    {
                        sb.Append(_imp);
                        sb.Append(": ");
                    }
                    else
                    {
                        sb.Append(_imp);
                        sb.Append(":\n");
                    }                    
                }
                else
                {
                    //Console.WriteLine(formatStr, a, b);
                }

                sb.AppendFormat(formatStr, a, b);

                OutputToConsole(sb);

                sb.Clear();

                if (_printCallstack)
                    Console.WriteLine(Environment.StackTrace);

                if (!_active)
                    return;

                if (_htmlWriter == null)
                    return;

                // jcf: generates garbage! should be converted to take a StringBuilder, probably
                OutputHtml(string.Format(formatStr, a, b), _cat, _printCallstack, _imp);
            }

            public void Emit<A, B, C>(string formatStr, A a, B b, C c)
#if WINRT
                where A : IComparable
                where B : IComparable
                where C : IComparable
#else
                where A : IConvertible
                where B : IConvertible
                where C : IConvertible
#endif
            {
                if (DebugTools._sb == null)
                    return;
                var sb = DebugTools._sb;

                if (_imp >= Importance.WARNING)
                {
                    bool isSingleLine = !formatStr.Contains('\n');
                    if (isSingleLine)
                    {
                        sb.Append(_imp);
                        sb.Append(": ");
                    }
                    else
                    {
                        sb.Append(_imp);
                        sb.Append(":\n");
                    }
                }
                else
                {
                    //Console.WriteLine(formatStr, a, b);
                }

                sb.AppendFormat(formatStr, a, b, c);

                OutputToConsole(sb);

                sb.Clear();

                if (_printCallstack)
                    Console.WriteLine(Environment.StackTrace);

                if (!_active)
                    return;

                if (_htmlWriter == null)
                    return;

                // jcf: generates garbage! should be converted to take a StringBuilder, probably
                OutputHtml(string.Format(formatStr, a, b, c), _cat, _printCallstack, _imp);
            }

            public void Emit<A, B, C, D>(string formatStr, A a, B b, C c, D d)
#if WINRT
                where A : IComparable
                where B : IComparable
                where C : IComparable
                where D : IComparable
#else
                where A : IConvertible
                where B : IConvertible
                where C : IConvertible
                where D : IConvertible
#endif
            {
                if (DebugTools._sb == null)
                    return;
                var sb = DebugTools._sb;

                if (_imp >= Importance.WARNING)
                {
                    bool isSingleLine = !formatStr.Contains('\n');
                    if (isSingleLine)
                    {
                        sb.Append(_imp);
                        sb.Append(": ");
                    }
                    else
                    {
                        sb.Append(_imp);
                        sb.Append(":\n");
                    }
                }
                else
                {
                    //Console.WriteLine(formatStr, a, b);
                }

                sb.AppendFormat(formatStr, a, b, c, d);

                OutputToConsole(sb);

                sb.Clear();

                if (_printCallstack)
                    Console.WriteLine(Environment.StackTrace);

                if (!_active)
                    return;

                if (_htmlWriter == null)
                    return;

                // jcf: generates garbage! should be converted to take a StringBuilder, probably
                OutputHtml(string.Format(formatStr, a, b, c, d), _cat, _printCallstack, _imp);
            }

            public void EmitDictionary<TKey, TVal>(Dictionary<TKey, TVal> dictToSerialize, string header)
            {
                WriteDictionary(dictToSerialize, header, _cat, _imp);
            }

            #endregion
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static LogOut? _Get(Categories cat = Categories.None, Importance importance = Importance.INFO)
        {
            var dbgCat = (DebugTools.Categories)cat;
            var dbgImp = (DebugTools.Importance)importance;

            if (!ShouldWrite(dbgCat, dbgImp))
                return null;

            bool printCallStack = ShouldPrintStackTrace(dbgImp);

            return new LogOut(dbgCat, dbgImp, printCallStack);
        }

        public static LogOut? Get(Categories cat = Categories.None, Importance importance = Importance.INFO)
        {
            return _Get(cat, importance);
        }

        public static LogOut? Verbose(Categories cat = Categories.None)
        {
            return _Get(cat, Importance.VERBOSE);
        }

        public static LogOut? Info(Categories cat = Categories.None)
        {
            return _Get(cat, Importance.INFO);
        }

        public static LogOut? Warn(Categories cat = Categories.None)
        {
            return _Get(cat, Importance.WARNING);
        }

        public static LogOut? Error(Categories cat = Categories.None)
        {
            return _Get(cat, Importance.ERROR);
        }

        #endregion

        // TODO: This should probably be moved into a separate file eventually if more code is written.
        #region CSS in log file
        private const string STYLING = @"<style>
        body 
        { 
            background-color: #1E1E1E; 
            color: #DCDCDC; 
            font-family: Arial;
        }
        .verbose { color: #7871D0; }
        .info { color: #3987D6; }
        .warning { color: #DCDCDC; }
        .error { color: #D69D85; }
        div.log { font-family: monospace; }
        div.log:hover { background-color: #333337; }
        #controls label:hover { background: #203F60; }
        table { border-collapse: collapse; }
        td { 
            border: white solid 1px; 
            min-width: 5em;
        }
        </style>";
        #endregion
        #region JavaScript in log file
        private const string JAVASCRIPT = @"
        <script type='text/javascript'>
        document.addEventListener('DOMContentLoaded', function onReady(event) {
            var checkboxes = document.querySelectorAll('#controls input[type=""checkbox""]');
            console.log('Ready!');
            for (var i = 0; i < checkboxes.length; ++i) {
                var box = checkboxes[i];
                console.log('Adding event listener to checkbox');
                box.addEventListener(""change"", function(event) {
                    var checked = event.target.checked;
                    var classToToggle = event.target.value;
                    var eltsToToggle = document.querySelectorAll('div.' + classToToggle);
                    for (var j = 0; j < eltsToToggle.length; ++j) {
                        eltsToToggle[j].style.display = checked ? 'block' : 'none';
                    }
                });
            }
        });
        </script>
        ";
        #endregion
        #region Interactive Header
        private static string CONTROLS = @"
        <div id='controls'>
        <label for='check_verbose'><input type='checkbox' value='verbose'    id='check_verbose' checked> Toggle Verbose Messages</label><br/>
        <label for='check_info'><input type='checkbox' value='info'    id='check_info' checked> Toggle Info</label><br/>
        <label for='check_warning'><input type='checkbox' value='warning' id='check_warning' checked> Toggle Warnings</label><br/>
        <label for='check_error'><input type='checkbox' value='error'   id='check_error' checked> Toggle Errors</label><br/>

        <!-- Auto-Generated from Enum -->
        {0}
        </div>
        ";
        #endregion

        // 512K default error log size. We want to avoid reallocating/resizing large blocks of memory, as this can cause SessionIO.Load to run out of memory.
        public const int DEFAULT_STREAM_BUFSIZ = 512000;
        public const string LOG_FILE = "log.html";
        public static bool[] ActiveCategories = new bool[(int)Categories.NumCategories];
        private static StreamWriter _htmlWriter;
        internal static Importance LoggingLevel = Importance.INFO;
        //private static Logger _logger = new Logger();

        public static bool ShouldWrite(Categories cat, Importance importance)
        {
            if (importance < LoggingLevel)
                return false;

            // jcf: I would think that anything of importance error (and maybe even warning) is significant enough to always get written
            //      regardless of 'active category', no?
            if (ActiveCategories[(int)cat] || cat == Categories.None || importance >= Importance.WARNING)
                return true;

            return false;
        }

        private static string WriteDiv(Categories cat, Importance importance)
        {
            string begin = "";
            string category = "";
            if (cat != Categories.None)
            {
                category = cat.ToString();
            }
            switch (importance)
            {
                case Importance.ERROR: begin = "<div class=\"log error " + category + "\">"; break;
                case Importance.INFO: begin = "<div class=\"log info " + category + "\">"; break;
                case Importance.WARNING: begin = "<div class=\"log warning " + category + "\">"; break;
                case Importance.VERBOSE: begin = "<div class=\"log verbose " + category + "\">"; break;
            }
            return begin;
        }

        #region Write(...)/Info/Warn/Error - Avoids Garbage collection        

        /// <summary>
        /// Use for errors that don't require formatting.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="cat"></param>
        public static void Error(string str, Categories cat = Categories.None)
        {
            WriteLine(str, cat, false, Importance.ERROR);
        }

        /// <summary>
        /// Use for errors that don't require *much* formatting.
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="cat"></param>
        public static void Error(string summary, Exception e, Categories cat = Categories.None)
        {
#if CONSOLE
            Write("{0}:\n{1}", summary, e.ToString(), cat, Importance.ERROR);
#else
            WriteLine(string.Format("<details><summary>{0}</summary><pre>{1}</pre></details>", summary, e.ToString()), cat, false, Importance.ERROR);
#endif
        }

        /// <summary>
        /// Use for warnings that don't require formatting.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="cat"></param>
        public static void Warn(string str, Categories cat = Categories.None)
        {
            WriteLine(str, cat, false, Importance.WARNING);
        }

        /// <summary>
        /// Use for generic messages that don't require formatting.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="cat"></param>
        public static void Info(string str, Categories cat = Categories.None)
        {
            if (!ShouldWrite(cat, Importance.INFO))
                return;

            WriteLine(str, cat, false, Importance.INFO);
        }

        /// <summary>
        /// Use for generic messages that don't require formatting.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="cat"></param>
        public static void Verbose(string str, Categories cat = Categories.None)
        {
            if (!ShouldWrite(cat, Importance.VERBOSE))
                return;

            WriteLine(str, cat, false, Importance.VERBOSE);
        }

        private static bool ShouldPrintStackTrace(Importance i)
        {
            return i > Importance.WARNING;
        }

        public static void Write<A>(A a, Categories cat, Importance importance)
        {
            if (!ShouldWrite(cat, importance))
                return;

            // jcf: there should be no string.Format calls anywhere use a persistent stringbuilder and pass it around
            WriteLine(a.ToString(), cat, ShouldPrintStackTrace(importance), importance);
        }

        /// <summary>
        /// For all other log messages that require formatting.
        /// </summary>
        public static void Write<A>(string formatStr, A a, Categories cat, Importance importance)
        {
            if (!ShouldWrite(cat, importance))
                return;

            // jcf: there should be no string.Format calls anywhere use a persistent stringbuilder and pass it around
            WriteLine(string.Format(formatStr, a), cat, ShouldPrintStackTrace(importance), importance);
        }

        public static void Write<A,B>(string formatStr, A a, B b, Categories cat, Importance importance)
        {
            if (!ShouldWrite(cat, importance))
                return;

            // jcf: there should be no string.Format calls anywhere use a persistent stringbuilder and pass it around
            WriteLine(string.Format(formatStr, a, b), cat, ShouldPrintStackTrace(importance), importance);
        }

        public static void Write<A, B, C>(string formatStr, A a, B b, C c, Categories cat, Importance importance)
        {
            if (!ShouldWrite(cat, importance))
                return;

            // jcf: there should be no string.Format calls anywhere use a persistent stringbuilder and pass it around
            WriteLine(string.Format(formatStr, a, b, c), cat, ShouldPrintStackTrace(importance), importance);
        }

        public static void Write<A, B, C, D>(string formatStr, A a, B b, C c, D d, Categories cat, Importance importance)
        {
            if (!ShouldWrite(cat, importance))
                return;

            // jcf: there should be no string.Format calls anywhere use a persistent stringbuilder and pass it around
            WriteLine(string.Format(formatStr, a, b, c, d), cat, ShouldPrintStackTrace(importance), importance);
        }

        public static void Write<A, B, C, D, E>(string formatStr, A a, B b, C c, D d, E e, Categories cat, Importance importance)
        {
            if (!ShouldWrite(cat, importance))
                return;

            // jcf: there should be no string.Format calls anywhere use a persistent stringbuilder and pass it around
            WriteLine(string.Format(formatStr, a, b, c, d, e), cat, ShouldPrintStackTrace(importance), importance);
        }
        public static void Write<A, B, C, D, E, F>(string formatStr, A a, B b, C c, D d, E e, F f, Categories cat, Importance importance)
        {
            if (!ShouldWrite(cat, importance))
                return;

            // jcf: there should be no string.Format calls anywhere use a persistent stringbuilder and pass it around
            WriteLine(string.Format(formatStr, a, b, c, d, e, f), cat, ShouldPrintStackTrace(importance), importance);
        }
        public static void Write<A, B, C, D, E, F, G>(string formatStr, A a, B b, C c, D d, E e, F f, G g, Categories cat, Importance importance)
        {
            if (!ShouldWrite(cat, importance))
                return;

            // jcf: there should be no string.Format calls anywhere use a persistent stringbuilder and pass it around
            WriteLine(string.Format(formatStr, a, b, c, d, e, f, g), cat, ShouldPrintStackTrace(importance), importance);
        }
        public static void Write<A, B, C, D, E, F, G, H>(string formatStr, A a, B b, C c, D d, E e, F f, G g, H h, Categories cat, Importance importance)
        {
            if (!ShouldWrite(cat, importance))
                return;

            // jcf: there should be no string.Format calls anywhere use a persistent stringbuilder and pass it around
            WriteLine(string.Format(formatStr, a, b, c, d, e, f, g, h), cat, ShouldPrintStackTrace(importance), importance);
        }
        public static void Write<A, B, C, D, E, F, G, H, I>(string formatStr, A a, B b, C c, D d, E e, F f, G g, H h, I i, Categories cat, Importance importance)
        {
            if (!ShouldWrite(cat, importance))
                return;

            // jcf: there should be no string.Format calls anywhere use a persistent stringbuilder and pass it around
            WriteLine(string.Format(formatStr, a, b, c, d, e, f, g, h, i), cat, ShouldPrintStackTrace(importance), importance);
        }
        public static void Write<A, B, C, D, E, F, G, H, I, J>(string formatStr, A a, B b, C c, D d, E e, F f, G g, H h, I i, J j, Categories cat, Importance importance)
        {
            if (!ShouldWrite(cat, importance))
                return;

            // jcf: there should be no string.Format calls anywhere use a persistent stringbuilder and pass it around
            WriteLine(string.Format(formatStr, a, b, c, d, e, f, g, h, i, j), cat, ShouldPrintStackTrace(importance), importance);
        }
        public static void Write<A, B, C, D, E, F, G, H, I, J, K>(string formatStr, A a, B b, C c, D d, E e, F f, G g, H h, I i, J j, K k, Categories cat, Importance importance)
        {
            if (!ShouldWrite(cat, importance))
                return;

            // jcf: there should be no string.Format calls anywhere use a persistent stringbuilder and pass it around
            WriteLine(string.Format(formatStr, a, b, c, d, e, f, g, h, i, j, k), cat, ShouldPrintStackTrace(importance), importance);
        }
        public static void Write<A, B, C, D, E, F, G, H, I, J, K, L>(string formatStr, A a, B b, C c, D d, E e, F f, G g, H h, I i, J j, K k, L l, Categories cat, Importance importance)
        {
            if (!ShouldWrite(cat, importance))
                return;

            // jcf: there should be no string.Format calls anywhere use a persistent stringbuilder and pass it around
            WriteLine(string.Format(formatStr, a, b, c, d, e, f, g, h, i, j, k, l), cat, ShouldPrintStackTrace(importance), importance);
        }

        #endregion

        // jcf: this should be taking the format string and arguments not the already-formatted-string
        public static void WriteLine(string str, Categories cat = Categories.None, bool printStackTrace = false, Importance importance = Importance.INFO)
        {
            // NOTE: no need to check this twice
            //if (!ShouldWrite(cat, importance))
            //    return;
            
#if DEBUG
            OutputToConsole(str, cat, importance);
            if (printStackTrace)
                Console.WriteLine(Environment.StackTrace);
#endif

            if (!_active)
                return;

            if (_htmlWriter == null)
                return;

            // jcf: this should be taking the format string and arguments not the already-formatted-string
            OutputHtml(str, cat, printStackTrace, importance);            
        }

        // jcf: this should be taking the format string and arguments not the already-formatted-string
        private static void OutputHtml(string str, Categories cat = Categories.None, bool printStackTrace = false, Importance importance = Importance.INFO)
        {
            // jcf: this should be using a [ThreadStatic]StringBuilder

            string div = WriteDiv(cat, importance);
            string trace = "";

            if (printStackTrace)
            {
                trace += "  <details style=\"display: inline-block;\"><summary>" + str + "</summary><pre>" + Environment.StackTrace + "</pre></details>";
                str = div + System.DateTime.Now.ToLongTimeString() + " : " + trace + "</div>";
            }
            else
            {
                str = div + System.DateTime.Now.ToLongTimeString() + " : " + str + "</div>";
            }
            OutputToStream(str);
        }

        [ThreadStatic]
        private static StringBuilder _sb = new StringBuilder(256);

        [ThreadStatic]
        private static char[] _tmp = new char[256];

        public static void WriteDictionary<TKey,TVal>(Dictionary<TKey, TVal> dictToSerialize, string header, Categories cat = Categories.None, Importance importance = Importance.INFO)
        {
            if (!ShouldWrite(cat, importance))
                return;

            if (dictToSerialize == null)
                return;

#if DEBUG && BLUEFIN
            // write to console
            {
                var sb = _sb;
                sb.AppendFormatEx("[{0}]", importance);

                if (cat != Categories.None)
                {
                    sb.AppendFormatEx("[{0}]", cat);
                }

                sb.Append(": ");
                sb.Append(header);
                sb.AppendLine();

                foreach (var kvp in dictToSerialize)
                {
                    sb.Append("\t");

                    sb.Append("[");
                    sb.Append(kvp.Key);
                    sb.Append("]");

                    sb.Append(" = ");

                    sb.Append("[");
                    sb.Append(kvp.Value);
                    sb.Append("]\n");

                    //sb.AppendFormatEx("[{0}] = {1}\n", kvp.Key, kvp.Value != null ? kvp.Value.ToString() : null);
                }

                sb.AppendLine();

                OutputToConsole(sb);                

                sb.Clear();

                // jcf: this is very slow, especially considering this gets called about 20 times when creating a lobby
                //var stack = new StackTrace(1);
                //Console.WriteLine(stack);
                // Agreed. Waaaaay too slow. -- Dex
            }
#endif

            if (!_active)
                return;

            if (_htmlWriter != null)
            {
                string begin = WriteDiv(cat, importance);
                StringBuilder tableData = new StringBuilder();
                foreach (var kvp in dictToSerialize)
                {
                    tableData.AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>", kvp.Key, kvp.Value != null ? kvp.Value.ToString() : null);
                }
                string trace = String.Format(@"<details style=""display: inline-block;""><summary>{0}</summary><table>
            <thead>
                <tr>
                    <td>Key</td>
                    <td>Value</td>
                </tr>
            </thead>
            <tbody>
                {2}
            </tbody>
            </table><pre>{1}</pre></details>", header, Environment.StackTrace, tableData.ToString());
                header = begin + System.DateTime.Now.ToLongTimeString() + " : " + trace + "</div>";
                OutputToStream(header);
            }
        }
        
        // jcf: not ported to use 'ShouldWrite' nor to write to console output in debug builds, because 0 references
        /*
        public static void WriteCompareObjects<T>(string desc, T[] objs, string[] names, Categories cat = Categories.None, Importance importance = Importance.INFO) 
        {
            if (importance < LoggingLevel)
                return;

            if (objs.Length == 0)
                return;

            if (ActiveCategories[(int)cat] || cat == Categories.None)
            {
#if DEBUG
                Console.WriteLine(desc);
                Debug.Assert(names.Length == objs.Length);
#endif

                if (!_active) return;
                string begin = WriteDiv(cat, importance);
                StringBuilder tableData = new StringBuilder();

                foreach (var field in typeof(T).GetFields())
                {
                    tableData.Append("<tr>");
                    for (int i = 0; i < objs.Length; ++i)
                    {
                        var value = typeof(T).GetField(field.Name).GetValue(objs[i]);
                        string strVal = "";
                        if (value != null) {
                            strVal = value.ToString();
                        }
                        tableData.AppendFormat("<td>{0}<td><td>{1}</td>", field.Name, strVal);
                    }
                    tableData.Append("</tr>");
                }

                string cols = "";
                for (int i = 0; i < objs.Length; ++i)
                    cols += "<td>Key</td><td>Value</td>";
                string labels = "";
                for (int i = 0; i < names.Length; ++i)
                {
                    labels += "<td colspan=\"2\">" + names[i] + "</td>";
                }

                string trace = String.Format(@"<details style=""display: inline-block;""><summary>{0}</summary><table>
                <thead>
                    <tr>{4}</tr>
                    <tr>
                        {3}
                    </tr>
                </thead>
                <tbody>
                    {2}
                </tbody>
                </table><pre>{1}</pre></details>", desc, Environment.StackTrace, tableData.ToString(), cols, labels);
                desc = begin + System.DateTime.Now.ToLongTimeString() + " : " + trace + "</div>";
                Output(desc);
            }
        }
        */

        public static void Assert(bool expression, string failureMessage)
        {
#if DEBUG
            if (!expression)
            {
                throw new Exception(failureMessage);
            }
#else
            if (!expression)
            {
                WriteLine(failureMessage, Categories.None, true, Importance.ERROR);
            }
#endif
        }

        public static void WriteNestedException(Exception e)
        {
            WriteLine(e.ToString(), Categories.None, true, Importance.ERROR);
            if(e.InnerException != null)
            {
                WriteLine("----- InnerException -----");
                WriteNestedException(e.InnerException);
            }
        }

        public static void SetCategoryActive(Categories cat, bool active)
        {
            ActiveCategories[(int) cat] = active;
        }


        static protected bool              _active;  // In case you want to deactivate the logger
 
        static public void Init(string logFileDir, string logFileName)
        {
            _active = true;
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
			_logFileName = logFileName;

#if BLUEFIN
            //if (Filesystem.LoadCachedContent)
            //   logStream = new StreamWriter(File.Create("/data/" + LOG_FILE));
            //else
            //   logStream = new StreamWriter(new MemoryStream());
#else
			string saveFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string applicationFolder = Path.Combine(saveFolder, logFileDir);
            string crashFolder = Path.Combine(applicationFolder, "crash");
            string filename = Path.Combine(saveFolder, _logFileName, LOG_FILE);
            if (!Directory.Exists(applicationFolder))
            {
                // Linux does not like creating a new folder and a new file at the same time.
                Directory.CreateDirectory(applicationFolder);
            }
            if (!Directory.Exists(crashFolder))
            {
                Directory.CreateDirectory(crashFolder);
            }
#if !DEBUG
            // jcf: and what happens after its full?
            _htmlWriter = new StreamWriter(new MemoryStream(DEFAULT_STREAM_BUFSIZ));   
#else
            _htmlWriter = new StreamWriter(new FileStream(filename, FileMode.Create, FileAccess.Write));
#endif
#endif
            if (_htmlWriter != null)
            {
                // Auto-generate the remaining checkboxes from the enum
                string autogen = "";
                foreach (Categories value in Enum.GetValues(typeof(Categories)))
                {
                    if (value == Categories.NumCategories || value == Categories.None)
                        continue;
                    autogen += String.Format("<label for='check_{0}' ><input type='checkbox' value='{0}' id='check_{0}' checked/> Toggle {0}</label><br/>", value);
                }
                CONTROLS = String.Format(CONTROLS, autogen);

                _htmlWriter.WriteLine(STYLING);
                _htmlWriter.WriteLine(JAVASCRIPT);
                _htmlWriter.WriteLine("<body>");
                _htmlWriter.WriteLine("<h1>Log File</h1>");
                _htmlWriter.WriteLine("<span style=\"font-family: &quot;Kootenay&quot;; color: #C9CB62;\">");
                _htmlWriter.WriteLine("Log started at " + System.DateTime.Now.ToLongTimeString() + "</span><hr/>");
                _htmlWriter.WriteLine(CONTROLS + "<hr/>");
                _htmlWriter.Flush();
            }
        }
 
        static public bool Active
        {
            get { return _active; }
            set { _active = value; }
        }
 
        static private void OutputToStream(string text)
        {
            if (_htmlWriter != null)
            {
                try
                {
                    _htmlWriter.WriteLine(text);
                    _htmlWriter.Flush();
                }
                catch (System.Exception e)
                {
                    // TODO: If we get an error here (e.g. on Release) it could be because of memory, and I'd rather not silently fail.
                    string error = e.Message;
                }
            }
        }

        static private void OutputToConsole(StringBuilder sb)
        {
            var lenreq = sb.Length + 1;
            if (_tmp.Length < lenreq)
                _tmp = new char[lenreq];

            var tmp = _tmp;
            sb.CopyTo(0, tmp, 0, sb.Length);
            //tmp[sb.Length] = (char)0;

            Console.WriteLine(tmp, 0, sb.Length);
        }

        static private void OutputToConsole(string text, Categories cat, Importance importance)
        {
            if (importance >= Importance.WARNING)
            {
                bool isSingleLine = !text.Contains('\n');
                if (isSingleLine)
                {
                    Console.Write(importance);
                    Console.Write(": ");
                    Console.Write(text);
                    Console.Write("\n");
                }
                else
                {
                    Console.Write(importance);
                    Console.Write(":\n");
                    Console.Write(text);
                    Console.Write("\n");
                }
            }
            else
            {
                Console.WriteLine(text);
            }                     
        }

        static public void Flush(bool fatal=false) 
        {
            if (_htmlWriter != null)
            {
                string saveFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string filename = Path.Combine(saveFolder, _logFileName, LOG_FILE);
                _htmlWriter.WriteLine("</body>");
#if !DEBUG
                if (fatal)
                {
                    // Since we were often overwriting the log.html after a crash, save them to a subfolder crash/
                    string crashFile = Path.Combine(saveFolder, "ToothAndTail", "crash",
                        "crash-" + DateTime.Now.Ticks + ".html");
                    _htmlWriter.BaseStream.Seek(0, 0);
                    _htmlWriter.BaseStream.CopyTo(new FileStream(crashFile, FileMode.Create, FileAccess.Write));
                }
                _htmlWriter.BaseStream.Seek(0, 0);
                _htmlWriter.BaseStream.CopyTo(new FileStream(filename, FileMode.Create, FileAccess.Write));
#endif
                _htmlWriter.Close();
            }
        }

        public static void SetLoggingLevel(Importance logLevel)
        {
            LoggingLevel = logLevel;
        }

        public static string ListToString<T>(List<T> list)//, Action<T,StringBuilder> printItem)
        {
            if (list == null)
                return "NULL";
            if (list.Count == 0)
                return "{ EMPTY }";
            
            var sb = new StringBuilder();
            sb.Append("{ ");

            int count = list.Count;
            int i = 0;
            foreach (var item in list)
            {
                //printItem(item, sb);
                sb.Append(item);
                i++;

                if (i < count)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(" }");
            
            return sb.ToString();
        }

        public static string ArrayToString(sbyte[] array)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < array.Length; i++)
            {
                var v = array[i];
                sb.Append(v.ToString("X2"));
                if (i < (array.Length - 1))
                    sb.Append(" ");
            }
            return sb.ToString();
        }

        public static string ArrayToString(byte[] array)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < array.Length; i++)
            {
                var v = array[i];
                sb.Append(v.ToString("X2"));
                if (i < (array.Length - 1))
                    sb.Append(" ");
            }
            return sb.ToString();
        }

        private const string CommentFormat = "#----------------------------------------------------------------------------#";

        public static string FormatDivider(string label = null)
        {
            if (string.IsNullOrEmpty(label))
                return CommentFormat;

            label = " " + label + " ";
            var src = CommentFormat.Length / 2 - label.Length / 2;
            var dst = src + label.Length;

            return CommentFormat.Substring(0, src) + label + CommentFormat.Substring(dst);
        }

        private static int _mainThreadId = 0;

        [Conditional("VALIDATE_MAIN_THREAD_ENABLED")]
        public static void ValidateIsMainThread(bool req)
        {
            bool v = System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId;
            if (v != req)
            {
                Console.WriteLine(FormatDivider("ERROR: CODE EXECUTED ON UNSAFE THREAD!"));

                System.Diagnostics.Debugger.Break();

                // jcf: this just throws an exception.. but we want to force catastrophic failure
                //System.Threading.Thread.CurrentThread.Abort();

                System.Environment.Exit(666);
            }
        }

        /*
        public static void ValidateIsMainThread(bool req)
        {
#if FINAL
            // do nothing
#else
            bool v = System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId;
            if (v != req)
            {
                Console.WriteLine(FormatDivider("ERROR: CODE EXECUTED ON UNSAFE THREAD!"));

                System.Diagnostics.Debugger.Break();

                // jcf: this just throws an exception.. but we want to force catastrophic failure
                //System.Threading.Thread.CurrentThread.Abort();

                System.Environment.Exit(666);
            }
#endif      
        }
        */

        public static bool IsMainThread()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        }

        public static void Fatal(string message)
        {
            // If we're fortunate enough to be running out of a debugger, try to set a breakpoint.
            System.Diagnostics.Debugger.Break();

            // If not (e.g. release build for customer) print out.
            Console.WriteLine(message); // always accessible on a ps4

            // If we're failing on PC, try to get this error into the log.html
            try
            {
                DebugTools.Error(message);
                DebugTools.Flush(true);
            }
            catch
            {
                // If this fails, ignore it.
            }

            Environment.Exit(0xDEAD);
        }

        // As in, "fatal, in shipping version".
        public static void FatalISV(string message)
        {
            // If we're fortunate enough to be running out of a debugger, try to set a breakpoint.
            System.Diagnostics.Debugger.Break();

            // If not (e.g. release build for customer) print out.
            Console.WriteLine(message); // always accessible on a ps4

            // If we're failing on PC, try to get this error into the log.html
            try
            {
                DebugTools.Error(message);
                DebugTools.Flush();
            }
            catch
            {
                // If this fails, ignore it.
            }

            Environment.Exit(0xDEAD);
        }
    }
}
