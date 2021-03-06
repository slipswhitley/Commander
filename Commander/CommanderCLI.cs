﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Commander {

    /// <summary>
    /// Reads Input from a TextReader and command arguments and flags
    /// Supports string quotation (") and backslash escape character
    /// </summary>
    public class CommanderCLI {

        private bool readFlags = true;

        /// <summary>
        /// Should the CLI Automatically read and output flags or treat them as arguments
        /// </summary>
        public bool ReadFlags {
            get {
                return readFlags;
            }
            set {
                readFlags = value;
            }
        }

        /// <summary>
        /// If True, an unescaped "Null" argument will passed as null
        /// </summary>
        public bool ParseUnescapedNullsAsNull { get; set; }

        /// <summary>
        /// The TextReader
        /// </summary>
        public TextReader Reader { get; private set; }

        /// <summary>
        /// Returns the StringBuilder for the last command as raw
        /// </summary>
        public StringBuilder LastCommandAsRaw;

        /// <summary>
        /// A text writer to pipe reader output too
        /// </summary>
        [Obsolete]
        public TextWriter Writer {
            get {
                return Writers.Count > 0 ? Writers[0] : null;
            }
            set {
                if (value == null) {
                    Writers.Clear();
                    return;
                }

                Writers.Clear();
                Writers.Add(value);
            }
        }

        private List<TextWriter> _writers;
        /// <summary>
        /// Text Writer list to pipe reader output too
        /// </summary>
        public List<TextWriter> Writers {
            get {
                if (_writers == null) {
                    _writers = new List<TextWriter>();
                }
                return _writers;
            }
        }


        /// <summary>
        /// New CommanderCLI using Console.In
        /// </summary>
        public CommanderCLI() :
            this(Console.In) {
        }

        /// <summary>
        /// New CommanderCLI using Custom Reader
        /// </summary>
        /// <param name="reader">The TextReader</param>
        public CommanderCLI(TextReader reader) {
            this.Reader = reader;
        }

        /// <summary>
        /// Reads the next unquoted, unescaped new line character
        /// and outputs command Flags / arguments
        /// </summary>
        /// <param name="args">TextReader Read Operation Arguments</param>
        /// <param name="flags">TextReader Read Operation Flags</param>
        /// <returns></returns>
        public bool ReadLine(out string[] args, out string[] flags) {
            LastCommandAsRaw = new StringBuilder();

            // Args List / Flags List
            var argsList = new List<string>();
            var flagsList = new List<string>();

            // Save argument during before loop condition to prevent last
            // argument from being skipped.
            bool saveArg() {
                bool exit = ReadArg(out string arg, out bool stringReadingUsed);

                if (ParseUnescapedNullsAsNull && arg.ToLower() == "null" && !stringReadingUsed) {
                    argsList.Add(null);
                    return exit;
                }

                // Support flags if, matches flag format and stringReading was not used
                if (arg.Length > 2 && arg.StartsWith("--") && ReadFlags
                    && !stringReadingUsed) {
                    flagsList.Add(arg.Substring(2));
                }
                else {
                    argsList.Add(arg);
                }

                return exit;
            }

            // Run the Read Loop
            while (saveArg()) ;

            // Output Args / Flags
            args = argsList.ToArray();
            flags = flagsList.ToArray();

            // Return false if first argument is exit
            return args.Length < 1 || args[0]?.ToLower() != "exit";
        }

        protected bool ReadArg(out string arg, out bool stringReadingUsed) {
            stringReadingUsed = false;

            // First Read Defaults
            var stringReading = false;
            var escapeEnabled = false;
            var thisArg = new StringBuilder();

            while (true) {
                var thisCharVal = Reader.Read();
                var thisChar = (char) thisCharVal;

                // Writer Output
                if (thisCharVal != -1) {
                    Writers.ForEach(writer => writer.Write(thisChar));
                    LastCommandAsRaw.Append(thisChar);
                }

                // Escaped characters are added to buffer before additional processing
                if (escapeEnabled) {
                    thisArg.Append(thisChar);
                    escapeEnabled = false;
                    continue;
                }
                if (thisChar == '\\') {
                    escapeEnabled = true;
                    continue;
                }

                // Quoted string support
                if (thisChar == '"') {
                    stringReading = !stringReading;
                    stringReadingUsed = true;
                    continue;
                }
                if (stringReading) {
                    thisArg.Append(thisChar);
                    continue;
                }

                // Tab AutoComplete
                if (thisChar == '\t') {
                    // todo Tab AutoCompelete
                    continue;
                }

                // Ignore unescaped return carages
                if (thisChar == '\r') {
                    continue;
                }

                // Finiaize Argument on Space / NewLine
                // Returns false on NewLine (Stop Reading)
                // NOTE: Only exit point for this method
                if (thisChar == ' ' || thisChar == '\n') {
                    arg = thisArg.ToString();
                    return thisChar == ' ';
                }

                // No rules match, Read character as is
                thisArg.Append(thisChar);
            }
        }
    }
}