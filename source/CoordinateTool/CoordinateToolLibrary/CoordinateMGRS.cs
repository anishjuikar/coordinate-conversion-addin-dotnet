﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoordinateToolLibrary.Models;
using System.Text.RegularExpressions;
using System.Globalization;

namespace CoordinateToolLibrary
{
    public class CoordinateMGRS : CoordinateBase
    {
        public CoordinateMGRS() 
        {
            GZD = string.Empty;
            GS = string.Empty;
            Easting = 0;
            Northing = 0;
        }

        // grid zone, grid square, easting, northing 5 digits is 1m
        public CoordinateMGRS(string gzd, string gsquare, int easting, int northing)
        {
            GZD = gzd;
            GS = gsquare;
            Northing = northing;
            Easting = easting;
        }

        public string GZD { get; set; }
        public string GS { get; set; }
        public int Easting { get; set; }
        public int Northing { get; set; }

        #region Methods

        public static bool TryParse(string input, out CoordinateMGRS mgrs)
        {
            mgrs = new CoordinateMGRS();

            input = input.Trim();

            Regex regexMGRS = new Regex(@"^\s*(?<gzd>\d{1,2}[A-HJ-NP-Z])[ ]*(?<gs>[A-HJ-NP-Z]{2})[ ]*(?<numlocation>\d{0,10})\s*");

            var matchMGRS = regexMGRS.Match(input);

            if(matchMGRS.Success && matchMGRS.Length == input.Length)
            {
                if (ValidateNumericCoordinateMatch(matchMGRS, new string[] { "numlocation" }))
                {
                    // need to validate the gzd and gs
                    try
                    {
                        mgrs.GZD = matchMGRS.Groups["gzd"].Value;
                        mgrs.GS = matchMGRS.Groups["gs"].Value;
                        var tempEN = matchMGRS.Groups["numlocation"].Value;
                        if (tempEN.Length % 2 == 0 && tempEN.Length > 0)
                        {
                            int numSize = tempEN.Length / 2;
                            mgrs.Easting = Int32.Parse(tempEN.Substring(0, numSize));
                            mgrs.Northing = Int32.Parse(tempEN.Substring(numSize, numSize));
                        }
                    }
                    catch
                    {
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        #endregion

        #region ToString

        public override string ToString(string format, IFormatProvider formatProvider)
        {
            var temp = base.ToString(format, formatProvider);

            if (!string.IsNullOrWhiteSpace(temp))
                return temp;

            var sb = new StringBuilder();

            if (format == null)
                format = "MGRS";

            NumberFormatInfo fi = NumberFormatInfo.InvariantInfo;

            switch (format.ToUpper())
            {
                case "":
                case "MGRS":
                    sb.Append(GZD);
                    sb.Append(GS);
                    sb.AppendFormat(fi, "{0:#}", this.Easting);
                    sb.AppendFormat(fi, "{0:#}", this.Northing);
                    break;
                default:
                    throw new Exception("CoordinateMGRS.ToString(): Invalid formatting string.");
            }

            return sb.ToString();
        }

        #endregion
    }

    public class CoordinateMGRSFormatter : CoordinateFormatterBase
    {
        public override string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg is CoordinateMGRS)
            {
                if (string.IsNullOrWhiteSpace(format))
                {
                    return this.Format("ZSE#N#", arg, this);
                }
                else
                {
                    var coord = arg as CoordinateMGRS;
                    var cnum = coord.Easting;
                    var sb = new StringBuilder();
                    var olist = new List<object>();
                    bool startIndexNeeded = false;
                    bool endIndexNeeded = false;
                    int currentIndex = 0;

                    foreach (char c in format.ToUpper())
                    {
                        if (startIndexNeeded && (c == '#' || c == '.' || c == '0'))
                        {
                            // add {<index>:
                            sb.AppendFormat("{{{0}:", currentIndex++);
                            startIndexNeeded = false;
                            endIndexNeeded = true;
                        }

                        if (endIndexNeeded && (c != '#' && c != '.' && c != '0'))
                        {
                            sb.Append("}");
                            endIndexNeeded = false;
                        }

                        switch (c)
                        {
                            case 'E': // longitude coordinate
                                cnum = coord.Easting;
                                olist.Add(Math.Abs(cnum));
                                startIndexNeeded = true;
                                break;
                            case 'N': // latitude coordinate
                                cnum = coord.Northing;
                                olist.Add(Math.Abs(cnum));
                                startIndexNeeded = true;
                                break;
                            case 'Z': // show +
                                sb.Append(coord.GZD);
                                break;
                            case 'S': // show -
                                sb.Append(coord.GS);
                                break;
                            default:
                                sb.Append(c);
                                break;
                        }
                    }

                    if (endIndexNeeded)
                    {
                        sb.Append("}");
                        endIndexNeeded = false;
                    }

                    return String.Format(sb.ToString(), olist.ToArray());

                }
            }

            if (arg is IFormattable)
            {
                return ((IFormattable)arg).ToString(format, formatProvider);
            }
            else
            {
                return arg.ToString();
            }
        }
    }

}
