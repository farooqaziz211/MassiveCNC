﻿/* MassiveCNC Playground. An Unity3D based framework for controller CNC-based machines.
    Created and altered by Max Malherbe.
    
    Originally created by Sven Hasemann, altered and rewritten by me.

    Origibal Project : GRBL-Plotter. Another GCode sender for GRBL.
    This file is part of the GRBL-Plotter application.
   
    Copyright (C) 2019 Sven Hasemann contact: svenhb@web.de

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/using System;
using System.Collections.Generic;
using static Assets.Scripts.Dimensions;
using Assets.Scripts.ImageProcessor;
using Color = UnityEngine.Color;
using System.Drawing;
using Point = System.Windows.Point;

namespace Assets.Scripts
{
    public enum grblState { idle, run, hold, jog, alarm, door, check, home, sleep, probe, unknown };

    public struct sConvert
    {
        public string msg;
        public string text;
        public grblState state;
        public Color color;
    };
    struct ArcProperties
    {
        public double angleStart, angleEnd, angleDiff, radius;
        public xyPoint center;
    };

    public class XYEventArgs : EventArgs
    {
        private double angle, scale;
        private xyPoint point;
        string command;
        public XYEventArgs(double a, double s, xyPoint p, string cmd)
        {
            angle = a;
            scale = s;
            point = p;
            command = cmd;
        }
        public XYEventArgs(double a, double x, double y, string cmd)
        {
            angle = a;
            point.X = x;
            point.Y = y;
            command = cmd;
        }
        public double Angle
        { get { return angle; } }
        public double Scale
        { get { return scale; } }
        public xyPoint Point
        { get { return point; } }
        public double PosX
        { get { return point.X; } }
        public double PosY
        { get { return point.Y; } }
        public string Command
        { get { return command; } }
    }

    public class XYZEventArgs : EventArgs
    {
        private double? posX, posY, posZ;
        string command;
        public XYZEventArgs(double? x, double? y, string cmd)
        {
            posX = x;
            posY = y;
            posZ = null;
            command = cmd;
        }
        public XYZEventArgs(double? x, double? y, double? z, string cmd)
        {
            posX = x;
            posY = y;
            posZ = z;
            command = cmd;
        }
        public double? PosX
        { get { return posX; } }
        public double? PosY
        { get { return posY; } }
        public double? PosZ
        { get { return posZ; } }
        public string Command
        { get { return command; } }
    }
    public class Dimensions
    {
        public double minx, maxx, miny, maxy, minz, maxz;
        public double dimx, dimy, dimz;

        public Dimensions()
        { resetDimension(); }
        public void setDimensionXYZ(double? x, double? y, double? z)
        {
            if (x != null) { setDimensionX((double)x); }
            if (y != null) { setDimensionY((double)y); }
            if (z != null) { setDimensionZ((double)z); }
        }
        public void setDimensionXY(double? x, double? y)
        {
            if (x != null) { setDimensionX((double)x); }
            if (y != null) { setDimensionY((double)y); }
        }
        public void setDimensionX(double value)
        {
            minx = Math.Min(minx, value);
            maxx = Math.Max(maxx, value);
            dimx = maxx - minx;
        }
        public void setDimensionY(double value)
        {
            miny = Math.Min(miny, value);
            maxy = Math.Max(maxy, value);
            dimy = maxy - miny;
        }
        public void setDimensionZ(double value)
        {
            minz = Math.Min(minz, value);
            maxz = Math.Max(maxz, value);
            dimz = maxz - minz;
        }

        public double getArea()
        { return dimx * dimy; }

        // calculate min/max dimensions of a circle
        public void setDimensionCircle(double x, double y, double radius, double start, double delta)
        {
            double end = start + delta;
            if (delta > 0)
            {
                for (double i = start; i < end; i += 5)
                {
                    setDimensionX(x + radius * Math.Cos(i / 180 * Math.PI));
                    setDimensionY(y + radius * Math.Sin(i / 180 * Math.PI));
                }
            }
            else
            {
                for (double i = start; i > end; i -= 5)
                {
                    setDimensionX(x + radius * Math.Cos(i / 180 * Math.PI));
                    setDimensionY(y + radius * Math.Sin(i / 180 * Math.PI));
                }
            }

        }
        public void resetDimension()
        {
            minx = Double.MaxValue;
            miny = Double.MaxValue;
            minz = Double.MaxValue;
            maxx = Double.MinValue;
            maxy = Double.MinValue;
            maxz = Double.MinValue;
            dimx = 0;
            dimy = 0;
            dimz = 0;
        }
        public struct xyPoint
        {
            public double X, Y;
            public xyPoint(double x, double y)
            { X = x; Y = y; }
            public xyPoint(Point xy)
            { X = xy.X; Y = xy.Y; }
            public xyPoint(xyPoint tmp)
            { X = tmp.X; Y = tmp.Y; }

            public xyPoint(xyzPoint tmp)
            { X = tmp.X; Y = tmp.Y; }
            public static explicit operator xyPoint(Point tmp)
            { return new xyPoint(tmp); }
            public static explicit operator xyPoint(xyzPoint tmp)
            { return new xyPoint(tmp); }
            public static explicit operator xyPoint(xyArcPoint tmp)
            { return new xyPoint(tmp.X, tmp.Y); }

            public Point ToPoint()
            { return new Point((int)X, (int)Y); }

            //       public static explicit operator System.Windows.Point(xyPoint tmp) => new System.Windows.Point(tmp.X,tmp.Y);

            public double DistanceTo(xyPoint anotherPoint)
            {
                double distanceCodeX = X - anotherPoint.X;
                double distanceCodeY = Y - anotherPoint.Y;
                return Math.Sqrt(distanceCodeX * distanceCodeX + distanceCodeY * distanceCodeY);
            }
            public double AngleTo(xyPoint anotherPoint)
            {
                double distanceX = anotherPoint.X - X;
                double distanceY = anotherPoint.Y - Y;
                double radius = Math.Sqrt(distanceX * distanceX + distanceY * distanceY);
                if (radius == 0) { return 0; }
                double cosinus = distanceX / radius;
                if (cosinus > 1) { cosinus = 1; }
                if (cosinus < -1) { cosinus = -1; }
                double angle = 180 * (float)(Math.Acos(cosinus) / Math.PI);
                if (distanceY > 0) { angle = -angle; }
                return angle;
            }

            // Overload + operator 
            public static xyPoint operator +(xyPoint b, xyPoint c)
            {
                xyPoint a = new xyPoint();
                a.X = b.X + c.X;
                a.Y = b.Y + c.Y;
                return a;
            }
            // Overload - operator 
            public static xyPoint operator -(xyPoint b, xyPoint c)
            {
                xyPoint a = new xyPoint();
                a.X = b.X - c.X;
                a.Y = b.Y - c.Y;
                return a;
            }
            // Overload * operator 
            public static xyPoint operator *(xyPoint b, double c)
            {
                xyPoint a = new xyPoint();
                a.X = b.X * c;
                a.Y = b.Y * c;
                return a;
            }
            // Overload / operator 
            public static xyPoint operator /(xyPoint b, double c)
            {
                xyPoint a = new xyPoint();
                a.X = b.X / c;
                a.Y = b.Y / c;
                return a;
            }
        };
        public class pState
        {
            public bool changed = false;
            public int motion = 0;           // {G0,G1,G2,G3,G38.2,G80} 
            public int feed_rate = 94;       // {G93,G94} 
            public int units = 21;           // {G20,G21} 
            public int distance = 90;        // {G90,G91} 
                                             // uint8_t distance_arc; // {G91.1} NOTE: Don't track. Only default supported. 
            public int plane_select = 17;    // {G17,G18,G19} 
                                             // uint8_t cutter_comp;  // {G40} NOTE: Don't track. Only default supported. 
            public double tool_length = 0;       // {G43.1,G49} 
            public int coord_select = 54;    // {G54,G55,G56,G57,G58,G59} 
                                             // uint8_t control;      // {G61} NOTE: Don't track. Only default supported. 
            public int program_flow = 0;    // {M0,M1,M2,M30} 
            public int coolant = 9;         // {M7,M8,M9} 
            public int spindle = 5;         // {M3,M4,M5} 
            public bool toolchange = false;
            public int tool = 0;            // tool number
            public double FR = 0;           // feedrate
            public double SS = 0;           // spindle speed
            public bool TLOactive = false;// Tool length offset

            public void reset()
            {
                motion = 0; plane_select = 17; units = 21;
                coord_select = 54; distance = 90; feed_rate = 94;
                program_flow = 0; coolant = 9; spindle = 5;
                toolchange = false; tool = 0; FR = 0; SS = 0;
                TLOactive = false; tool_length = 0;
                changed = false;
            }

        };

        public struct xyArcPoint
        {
            public double X, Y, CX, CY;
            public byte mode;
            public xyArcPoint(double x, double y, double cx, double cy, byte m)
            {
                X = x; Y = y; CX = cx; CY = cy; mode = m;
            }
            public xyArcPoint(xyPoint tmp)
            {
                X = tmp.X; Y = tmp.Y; CX = 0; CY = 0; mode = 0;
            }
            public xyArcPoint(Point tmp)
            {
                X = tmp.X; Y = tmp.Y; CX = 0; CY = 0; mode = 0;
            }
            public xyArcPoint(xyzPoint tmp)
            {
                X = tmp.X; Y = tmp.Y; CX = 0; CY = 0; mode = 0;
            }
         
            public static explicit operator xyArcPoint(xyzPoint tmp)
            {
                return new xyArcPoint(tmp);
            }
            public static explicit operator xyArcPoint(xyPoint tmp)
            {
                return new xyArcPoint(tmp);
            }
        }

        public xyPoint getCenter()
        {
            double cx = minx + ((maxx - minx) / 2);
            double cy = miny + ((maxy - miny) / 2);
            return new xyPoint(cx, cy);
        }

        // return string with dimensions
        public String getMinMaxString()
        {
            string x = String.Format("X:{0,8:####0.000} |{1,8:####0.000}\r\n", minx, maxx);
            string y = String.Format("Y:{0,8:####0.000} |{1,8:####0.000}\r\n", miny, maxy);
            string z = String.Format("Z:{0,8:####0.000} |{1,8:####0.000}", minz, maxz);
            if ((minx == Double.MaxValue) || (maxx == Double.MinValue))
                x = "X: unknown | unknown\r\n";
            if ((miny == Double.MaxValue) || (maxy == Double.MinValue))
                y = "Y: unknown | unknown\r\n";
            if ((minz == Double.MaxValue) || (maxz == Double.MinValue))
                z = "";// z = "Z: unknown | unknown";
            return "    Min.   | Max.\r\n" + x + y + z;
        }

        public struct xyzPoint
        {
            public double X, Y, Z, A, B, C;
            public xyzPoint(double x, double y, double z, double a = 0)
            { X = x; Y = y; Z = z; A = a; B = 0; C = 0; }
            // Overload + operator 
            public static xyzPoint operator +(xyzPoint b, xyzPoint c)
            {
                xyzPoint a = new xyzPoint();
                a.X = b.X + c.X;
                a.Y = b.Y + c.Y;
                a.Z = b.Z + c.Z;
                a.A = b.A + c.A;
                a.B = b.B + c.B;
                a.C = b.C + c.C;
                return a;
            }
            public static xyzPoint operator -(xyzPoint b, xyzPoint c)
            {
                xyzPoint a = new xyzPoint();
                a.X = b.X - c.X;
                a.Y = b.Y - c.Y;
                a.Z = b.Z - c.Z;
                a.A = b.A - c.A;
                a.B = b.B - c.B;
                a.C = b.C - c.C;
                return a;
            }
            public static bool AlmostEqual(xyzPoint a, xyzPoint b)
            {
                //     return (Math.Abs(a.X - b.X) <= grbl.resolution) && (Math.Abs(a.Y - b.Y) <= grbl.resolution) && (Math.Abs(a.Z - b.Z) <= grbl.resolution);
                return (gcode.isEqual(a.X, b.X) && gcode.isEqual(a.Y, b.Y) && gcode.isEqual(a.Z, b.Z));
            }

            public static class grbl
            {       // need to have global access to this data?
                public static xyzPoint posWCO = new xyzPoint(0, 0, 0);
                public static xyzPoint posWork = new xyzPoint(0, 0, 0);
                public static xyzPoint posMachine = new xyzPoint(0, 0, 0);
                public static bool posChanged = true;
                public static bool wcoChanged = true;

                public static bool isVersion_0 = true;  // note if grbl version <=0.9 or >=1.1
                        private static sConvert[] statusConvert = new sConvert[10];

                public static int axisCount = 0;
                public static bool axisA = false;       // axis A available?
                public static bool axisB = false;       // axis B available?
                public static bool axisC = false;       // axis C available?
                public static bool axisUpdate = false;  // update of GUI needed
                public static int RX_BUFFER_SIZE = 127; // grbl buffer size inside Arduino
                public static int pollInterval = 200;

                public static bool grblSimulate = false;
                private static Dictionary<int, float> settings = new Dictionary<int, float>();    // keep $$-settings
                private static Dictionary<string, xyzPoint> coordinates = new Dictionary<string, xyzPoint>();    // keep []-settings

                private static xyPoint _posMarker = new xyPoint(0, 0);
                private static double _posMarkerAngle = 0;
                private static xyPoint _posMarkerOld = new xyPoint(0, 0);
                public static xyPoint posMarker
                {
                    get
                    { return _posMarker; }
                    set
                    {
                        _posMarkerOld = _posMarker;
                        _posMarker = value;
                    }
                }
                public static xyPoint posMarkerOld
                {
                    get
                    { return _posMarkerOld; }
                    set
                    { _posMarkerOld = value; }
                }
                public static double posMarkerAngle
                {
                    get
                    { return _posMarkerAngle; }
                    set
                    { _posMarkerAngle = value; }
                }

                // Trace, Debug, Info, Warn, Error, Fatal
                //     private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

                // private mState machineState = new mState();     // Keep info about Bf, Ln, FS, Pn, Ov, A;
                //  private pState mParserState = new pState();     // keep info about last M and G settings
                //        public static pState parserState;
                //        public static bool isVers0 = true;
                //        public List<string> GRBLSettings = new List<string>();  // keep $$ settings

                public static double resolution = 0.000001;

                public static Dictionary<string, string> messageAlarmCodes = new Dictionary<string, string>();
                public static Dictionary<string, string> messageErrorCodes = new Dictionary<string, string>();
                public static Dictionary<string, string> messageSettingCodes = new Dictionary<string, string>();

                public static void init()   // initialize lists
                {

                    //    public enum grblState { idle, run, hold, jog, alarm, door, check, home, sleep, probe, unknown };
                    statusConvert[0].msg = "Idle"; statusConvert[0].text = ("grblIdle"); statusConvert[0].state = grblState.idle;
                    statusConvert[1].msg = "Run"; statusConvert[1].text = ("grblRun"); statusConvert[1].state = grblState.run; 
                    statusConvert[2].msg = "Hold"; statusConvert[2].text = ("grblHold"); statusConvert[2].state = grblState.hold; 
                    statusConvert[3].msg = "Jog"; statusConvert[3].text = ("grblJog"); statusConvert[3].state = grblState.jog; 
                    statusConvert[4].msg = "Alarm"; statusConvert[4].text = ("grblAlarm"); statusConvert[4].state = grblState.alarm; 
                    statusConvert[5].msg = "Door"; statusConvert[5].text = ("grblDoor"); statusConvert[5].state = grblState.door; 
                    statusConvert[6].msg = "Check"; statusConvert[6].text = ("grblCheck"); statusConvert[6].state = grblState.check; 
                    statusConvert[7].msg = "Home"; statusConvert[7].text = ("grblHome"); statusConvert[7].state = grblState.home; 
                    statusConvert[8].msg = "Sleep"; statusConvert[8].text = ("grblSleep"); statusConvert[8].state = grblState.sleep; 
                    statusConvert[9].msg = "Probe"; statusConvert[9].text = ("grblProbe"); statusConvert[9].state = grblState.probe; 

                    settings.Clear();
                    coordinates.Clear();
                }

                // store grbl settings https://github.com/gnea/grbl/wiki/Grbl-v1.1-Configuration#grbl-settings
                public static void setSettings(int id, string value)
                {
                    float tmp = 0;
                    if (float.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out tmp))
                    {
                        if (settings.ContainsKey(id))
                            settings[id] = tmp;
                        else
                            settings.Add(id, tmp);
                    }
                }
                public static float getSetting(int key)
                {
                    if (settings.ContainsKey(key))
                        return settings[key];
                    else
                        return -1;
                }

                // store gcode parameters https://github.com/gnea/grbl/wiki/Grbl-v1.1-Commands#---view-gcode-parameters
                public static void setCoordinates(string id, string value, string info)
                {
                    xyzPoint tmp = new xyzPoint();
                    string allowed = "PRBG54G55G56G57G58G59G28G30G92TLO";
                    if (allowed.Contains(id))
                    {
                        getPosition("abc:" + value, ref tmp);   // parse string [PRB:-155.000,-160.000,-28.208:1]
                        if (coordinates.ContainsKey(id))
                            coordinates[id] = tmp;
                        else
                            coordinates.Add(id, tmp);

                        if ((info.Length > 0) && (id == "PRB"))
                        {
                            xyzPoint tmp2 = new xyzPoint();
                            tmp2 = coordinates["PRB"];
                            tmp2.A = info == "1" ? 1 : 0;
                            coordinates["PRB"] = tmp2;
                        }
                    }
                }

                public static string displayCoord(string key)
                {
                    if (coordinates.ContainsKey(key))
                    {
                        if (key == "TLO")
                            return String.Format("                  {0,8:###0.000}", coordinates[key].Z);
                        else
                        {
                            string coordString = String.Format("{0,8:###0.000} {1,8:###0.000} {2,8:###0.000}", coordinates[key].X, coordinates[key].Y, coordinates[key].Z);
                            if (axisA) coordString = String.Format("{0} {1,8:###0.000}", coordString, coordinates[key].A);
                            if (axisB) coordString = String.Format("{0} {1,8:###0.000}", coordString, coordinates[key].B);
                            if (axisC) coordString = String.Format("{0} {1,8:###0.000}", coordString, coordinates[key].C);
                            return coordString;
                        }
                    }
                    else
                        return "no data";
                }
                public static xyzPoint getCoord(string key)
                {
                    if (coordinates.ContainsKey(key))
                        return coordinates[key];
                    return new xyzPoint();
                }

                public static bool getPRBStatus()
                {
                    if (coordinates.ContainsKey("PRB"))
                    { return (coordinates["PRB"].A == 0.0) ? false : true; }
                    return false;
                }

                private static void setMessageString(ref Dictionary<string, string> myDict, string resource)
                {
                    string[] tmp = resource.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    foreach (string s in tmp)
                    {
                        string[] col = s.Split(',');
                        string message = col[col.Length - 1].Trim('"');
                        myDict.Add(col[0].Trim('"'), message);
                    }
                }

                /// <summary>
                /// parse single gcode line to set parser state
                /// </summary>
                private static bool getTLO = false;
                public static void updateParserState(string line, ref pState myParserState)
                {
                    char cmd = '\0';
                    string num = "";
                    bool comment = false;
                    double value = 0;
                    getTLO = false;
                    myParserState.changed = false;

                    if (!(line.StartsWith("$") || line.StartsWith("("))) //do not parse grbl commands
                    {
                        try
                        {
                            foreach (char c in line)
                            {
                                if (c == ';')
                                    break;
                                if (c == '(')
                                    comment = true;
                                if (!comment)
                                {
                                    if (Char.IsLetter(c))
                                    {
                                        if (cmd != '\0')
                                        {
                                            value = 0;
                                            if (num.Length > 0)
                                            {
                                                try { value = double.Parse(num, System.Globalization.NumberFormatInfo.InvariantInfo); }
                                                catch { }
                                            }
                                            try { setParserState(cmd, value, ref myParserState); }
                                            catch { }
                                        }
                                        cmd = c;
                                        num = "";
                                    }
                                    else if (Char.IsNumber(c) || c == '.' || c == '-')
                                    { num += c; }
                                }
                                if (c == ')')
                                { comment = false; }
                            }
                            if (cmd != '\0')
                            {
                                try { setParserState(cmd, double.Parse(num, System.Globalization.NumberFormatInfo.InvariantInfo), ref myParserState); }
                                catch { }
                            }
                        }
                        catch { }
                    }
                }

                /// <summary>
                /// set parser state
                /// </summary>
                private static void setParserState(char cmd, double value, ref pState myParserState)
                {
                    //            myParserState.changed = false;
                    switch (Char.ToUpper(cmd))
                    {
                        case 'G':
                            if (value <= 3)
                            {
                                myParserState.motion = (byte)value;
                                break;
                            }
                            if ((value >= 17) && (value <= 19))
                                myParserState.plane_select = (byte)value;
                            else if ((value == 20) || (value == 21))
                                myParserState.units = (byte)value;
                            else if ((value >= 43) && (value < 44))
                            { myParserState.TLOactive = true; getTLO = true; }
                            else if (value == 49)
                                myParserState.TLOactive = false;
                            else if ((value >= 54) && (value <= 59))
                                myParserState.coord_select = (byte)value;
                            else if ((value == 90) || (value == 91))
                                myParserState.distance = (byte)value;
                            else if ((value == 93) || (value == 94))
                                myParserState.feed_rate = (byte)value;
                            myParserState.changed = true;
                            //                    MessageBox.Show("set parser state "+cmd + "  " + value.ToString()+ "  "+ myParserState.TLOactive.ToString());
                            break;
                        case 'M':
                            if ((value <= 2) || (value == 30))
                                myParserState.program_flow = (byte)value;    // M0, M1 pause, M2, M30 stop
                            else if ((value >= 3) && (value <= 5))
                                myParserState.spindle = (byte)value;    // M3, M4 start, M5 stop
                            else if ((value >= 7) && (value <= 9))
                                myParserState.coolant = (byte)value;    // M7, M8 on   M9 coolant off
                            else if (value == 6)
                                myParserState.toolchange = true;
                            myParserState.changed = true;
                            break;
                        case 'F':
                            myParserState.FR = value;
                            myParserState.changed = true;
                            break;
                        case 'S':
                            myParserState.SS = value;
                            myParserState.changed = true;
                            break;
                        case 'T':
                            myParserState.tool = (byte)value;
                            myParserState.changed = true;
                            break;
                        case 'Z':
                            if (getTLO)
                                myParserState.tool_length = value;
                            break;
                    }
                }
                // check https://github.com/gnea/grbl/wiki/Grbl-v1.1-Commands#g---view-gcode-parser-state
                public static int[] unknownG = { 41, 64, 81, 83 };
                public static grblState parseStatus(string status)    // {idle, run, hold, home, alarm, check, door}
                {
                    for (int i = 0; i < statusConvert.Length; i++)
                    {
                        if (status.StartsWith(statusConvert[i].msg))     // status == statusConvert[i].msg
                            return statusConvert[i].state;
                    }
                    return grblState.unknown;
                }
                public static string statusToText(grblState state)
                {
                    for (int i = 0; i < statusConvert.Length; i++)
                    {
                        if (state == statusConvert[i].state)
                        {
                            if (CNC_Settings.grblTranslateMessage)
                                return statusConvert[i].text;
                            else
                                return statusConvert[i].state.ToString();
                        }
                    }
                    return "Unknown";
                }
               
                public static void getPosition(string text, ref xyzPoint position)
                {
                    string[] dataField = text.Split(':');
                    string[] dataValue = dataField[1].Split(',');
                    //            axisA = false; axisB = false; axisC = false;
                    axisCount = 0;
                    if (dataValue.Length == 1)
                    {
                        Double.TryParse(dataValue[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out position.Z);
                        position.X = 0;
                        position.Y = 0;
                    }
                    if (dataValue.Length > 2)
                    {
                        Double.TryParse(dataValue[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out position.X);
                        Double.TryParse(dataValue[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out position.Y);
                        Double.TryParse(dataValue[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out position.Z);
                        axisCount = 3;
                    }
                    if (dataValue.Length > 3)
                    {
                        Double.TryParse(dataValue[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out position.A);
                        axisA = true; axisCount++;
                    }
                    if (dataValue.Length > 4)
                    {
                        Double.TryParse(dataValue[4], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out position.B);
                        axisB = true; axisCount++;
                    }
                    if (dataValue.Length > 5)
                    {
                        Double.TryParse(dataValue[5], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out position.C);
                        axisC = true; axisCount++;
                    }
                    //axisA = true; axisB = true; axisC = true;     // for test only
                }

                public static string getSetting(string msgNr)
                {
                    string msg = " no information found '" + msgNr + "'";
                    try { msg = grbl.messageSettingCodes[msgNr]; }
                    catch { }
                    return msg;
                }
                public static string getError(string rxString)
                {
                    string[] tmp = rxString.Split(':');
                    string msg = " no information found for error-nr. '" + tmp[1] + "'";
                    try
                    {
                        if (messageErrorCodes.ContainsKey(tmp[1].Trim()))
                        {
                            msg = grbl.messageErrorCodes[tmp[1].Trim()];
                            int errnr = Convert.ToInt16(tmp[1].Trim());
                            if ((errnr >= 32) && (errnr <= 34))
                                msg += "\r\n\r\nPossible reason: scale down of GCode with G2/3 commands.\r\nSolution: use more decimal places.";
                        }
                    }
                    catch { }
                    return msg;
                }
                public static bool errorBecauseOfBadCode(string rxString)
                {
                    string[] tmp = rxString.Split(':');
                    try
                    {
                        int[] notByGCode = { 3, 5, 6, 7, 8, 9, 10, 12, 13, 14, 15, 16, 17, 18, 19 };
                        int errnr = Convert.ToInt16(tmp[1].Trim());
                        if (Array.Exists(notByGCode, element => element == errnr))
                            return false;
                        else
                            return true;
                    }
                    catch { }
                    return true;
                }
                public static string getAlarm(string rxString)
                {
                    string[] tmp = rxString.Split(':');
                    string msg = " no information found for alarm-nr. '" + tmp[1] + "'";
                    try
                    {
                        if (messageAlarmCodes.ContainsKey(tmp[1].Trim()))
                            msg = grbl.messageAlarmCodes[tmp[1].Trim()];
                    }
                    catch { }
                    return msg;
                }
                public static string getRealtime(int id)
                {
                    switch (id)
                    {
                        case 24:
                            return "Soft-Reset";
                        case '?':
                            return "Status Report Query";
                        case '~':
                            return "Cycle Start / Resume";
                        case '!':
                            return "Feed Hold";
                        case 132:
                            return "Safety Door";
                        case 133:
                            return "Jog Cancel";
                        case 144:
                            return "Set 100% of programmed feed rate.";
                        case 145:
                            return "Feed Rate increase 10%";
                        case 146:
                            return "Feed Rate decrease 10%";
                        case 147:
                            return "Feed Rate increase 1%";
                        case 148:
                            return "Feed Rate decrease 1%";
                        case 149:
                            return "Set to 100% full rapid rate.";
                        case 150:
                            return "Set to 50% of rapid rate.";
                        case 151:
                            return "Set to 25% of rapid rate.";
                        case 153:
                            return "Set 100% of programmed spindle speed";
                        case 154:
                            return "Spindle Speed increase 10%";
                        case 155:
                            return "Spindle Speed decrease 10%";
                        case 156:
                            return "Spindle Speed increase 1%";
                        case 157:
                            return "Spindle Speed decrease 1%";
                        case 158:
                            return "Toggle Spindle Stop";
                        case 160:
                            return "Toggle Flood Coolant";
                        case 161:
                            return "Toggle Mist Coolant";
                        default:
                            return "unknown setting " + id.ToString();
                    }
                }
            }
            public string Print(bool singleLines, bool full = false)
            {
                bool ctrl4thUse = CNC_Settings.ctrl4thUse;
                string ctrl4thName = CNC_Settings.ctrl4thName;

                if (!full)
                {
                    if (ctrl4thUse || grbl.axisA)
                        if (singleLines)
                            return string.Format("X={0,9:0.000}\rY={1,9:0.000}\rZ={2,9:0.000}\r{3}={4,9:0.000}", X, Y, Z, ctrl4thName, A);
                        else
                            return string.Format("X={0,9:0.000}  Y={1,9:0.000}  Z={2,9:0.000}\r{3}={4,9:0.000}", X, Y, Z, ctrl4thName, A);

                    else
                        if (singleLines)
                        return string.Format("X={0,9:0.000}\rY={1,9:0.000}\rZ={2,9:0.000}", X, Y, Z);
                    else
                        return string.Format("X={0,9:0.000} Y={1,9:0.000} Z={2,9:0.000}", X, Y, Z);
                }
                else
                {
                    if (singleLines)
                        return string.Format("X={0,9:0.000}\rY={1,9:0.000}\rZ={2,9:0.000}\rA={3,9:0.000}\rB={4,9:0.000}\rC={5,9:0.000}", X, Y, Z, A, B, C);
                    else
                        return string.Format("X={0,9:0.000} Y={1,9:0.000} Z={2,9:0.000}\rA={3,9:0.000} B={4,9:0.000} C={5,9:0.000}", X, Y, Z, A, B, C);
                }
            }

        };



        public bool withinLimits(xyzPoint actualMachine, xyzPoint actualWorld)
        {
            return (withinLimits(actualMachine, minx - actualWorld.X, miny - actualWorld.Y) && withinLimits(actualMachine, maxx - actualWorld.X, maxy - actualWorld.Y));
        }
        public bool withinLimits(xyzPoint actualMachine, double tstx, double tsty)
        {
            double minlx = (double)CNC_Settings.machineLimitsHomeX;
            double maxlx = minlx + (double)CNC_Settings.machineLimitsRangeX;
            double minly = (double)CNC_Settings.machineLimitsHomeY;
            double maxly = minly + (double)CNC_Settings.machineLimitsRangeY;
            tstx += actualMachine.X;
            tsty += actualMachine.Y;
            if ((tstx < minlx) || (tstx > maxlx))
                return false;
            if ((tsty < minly) || (tsty > maxly))
                return false;
            return true;
        }
    }
    class gcodeMath
    {
        private static double precision = 0.00001;

        public static bool isEqual(Point a,Point b)
        { return ((Math.Abs(a.X - b.X) < precision) && (Math.Abs(a.Y - b.Y) < precision)); }
        public static bool isEqual(xyPoint a, xyPoint b)
        { return ((Math.Abs(a.X - b.X) < precision) && (Math.Abs(a.Y - b.Y) < precision)); }

        public static double distancePointToPoint(Point a, Point b)
        { return Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y))); }

        public static ArcProperties getArcMoveProperties(xyPoint pOld, xyPoint pNew, double? I, double? J, bool isG2)
        {
            ArcProperties tmp = getArcMoveAngle(pOld, pNew, I, J);
            if (!isG2) { tmp.angleDiff = Math.Abs(tmp.angleEnd - tmp.angleStart + 2 * Math.PI); }
            if (tmp.angleDiff > (2 * Math.PI)) { tmp.angleDiff -= (2 * Math.PI); }
            if (tmp.angleDiff < (-2 * Math.PI)) { tmp.angleDiff += (2 * Math.PI); }

            if ((pOld.X == pNew.X) && (pOld.Y == pNew.Y))
            {
                if (isG2) { tmp.angleDiff = -2 * Math.PI; }
                else { tmp.angleDiff = 2 * Math.PI; }
            }
            return tmp;
        }

        public static ArcProperties getArcMoveAngle(xyPoint pOld, xyPoint pNew, double? I, double? J)
        {
            ArcProperties tmp;
            if (I == null) { I = 0; }
            if (J == null) { J = 0; }
            double i = (double)I;
            double j = (double)J;
            tmp.radius = Math.Sqrt(i * i + j * j);  // get radius of circle
            tmp.center.X = pOld.X + i;
            tmp.center.Y = pOld.Y + j;
            tmp.angleStart = tmp.angleEnd = tmp.angleDiff = 0;
            if (tmp.radius == 0)
                return tmp;

            double cos1 = i / tmp.radius;
            if (cos1 > 1) cos1 = 1;
            if (cos1 < -1) cos1 = -1;
            tmp.angleStart = Math.PI - Math.Acos(cos1);
            if (j > 0) { tmp.angleStart = -tmp.angleStart; }

            double cos2 = (tmp.center.X - pNew.X) / tmp.radius;
            if (cos2 > 1) cos2 = 1;
            if (cos2 < -1) cos2 = -1;
            tmp.angleEnd = Math.PI - Math.Acos(cos2);
            if ((tmp.center.Y - pNew.Y) > 0) { tmp.angleEnd = -tmp.angleEnd; }

            tmp.angleDiff = tmp.angleEnd - tmp.angleStart - 2 * Math.PI;
            return tmp;
        }

        public static double getAlpha(Point pOld, double P2x, double P2y)
        { return getAlpha(pOld.X, pOld.Y, P2x, P2y); }
        public static double getAlpha(Point pOld, Point pNew)
        { return getAlpha(pOld.X, pOld.Y, pNew.X, pNew.Y); }
        public static double getAlpha(xyPoint pOld, xyPoint pNew)
        { return getAlpha(pOld.X, pOld.Y, pNew.X, pNew.Y); }
        public static double getAlpha(double P1x, double P1y, double P2x, double P2y)
        {
            double s = 1, a = 0;
            double dx = P2x - P1x;
            double dy = P2y - P1y;
            if (dx == 0)
            {
                if (dy > 0)
                    a = Math.PI / 2;
                else
                    a = 3 * Math.PI / 2;
                if (dy == 0)
                    return 0;
            }
            else if (dy == 0)
            {
                if (dx > 0)
                    a = 0;
                else
                    a = Math.PI;
                if (dx == 0)
                    return 0;
            }
            else
            {
                s = dy / dx;
                a = Math.Atan(s);
                if (dx < 0)
                    a += Math.PI;
            }
            return a;
        }

        public static double cutAngle = 0, cutAngleLast = 0, angleOffset = 0;
        public static void resetAngles()
        { angleOffset = cutAngle = cutAngleLast = 0.0; }
        public static double getAngle(Point a, Point b, double offset, int dir)
        { return monitorAngle(getAlpha(a, b) + offset, dir); }
        private static double monitorAngle(double angle, int direction)		// take care of G2 cw G3 ccw direction
        {
            double diff = angle - cutAngleLast + angleOffset;
            if (direction == 2)
            { if (diff > 0) { angleOffset -= 2 * Math.PI; } }    // clock wise, more negative
            else if (direction == 3)
            { if (diff < 0) { angleOffset += 2 * Math.PI; } }    // counter clock wise, more positive
            else
            {
                if (diff > Math.PI)
                    angleOffset -= 2 * Math.PI;
                if (diff < -Math.PI)
                    angleOffset += 2 * Math.PI;
            }
            angle += angleOffset;
            return angle;
        }


    }
}
