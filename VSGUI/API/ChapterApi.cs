using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;
using MessageBox = HandyControl.Controls.MessageBox;
using System.Windows;

// ****************************************************************************
// 
// Copyright (C) 2009  Jarrett Vance
// 
// code from http://jvance.com/pages/ChapterGrabber.xhtml
// 
// ****************************************************************************


namespace VSGUI.API
{
    [Serializable]

    public struct Chapter
    {
        public string Name { get; set; }

        [XmlIgnore]
        public TimeSpan Time { get; set; }

        public Chapter(Chapter oOther)
        {
            Name = oOther.Name;
            Time = oOther.Time;
        }

        // XmlSerializer does not support TimeSpan, so use this property for serialization instead
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public long TimeTicks
        {
            get { return Time.Ticks; }
            set { Time = new TimeSpan(value); }
        }

        //public string Lang { get; set; }
        public override string ToString()
        {
            return Time.ToString() + ": " + Name;
        }

        public void SetTimeBasedOnString(string strTimeCode)
        {
            if (strTimeCode.Length > 16)
                strTimeCode = strTimeCode.Substring(0, 16);

            if (TimeSpan.TryParse(strTimeCode, new System.Globalization.CultureInfo("en-US"), out TimeSpan result))
                Time = result;
        }

        public static Chapter ChangeChapterFPS(Chapter oChapter, double fpsIn, double fpsOut)
        {
            if (fpsIn == fpsOut || fpsIn == 0 || fpsOut == 0)
                return oChapter;

            double frames = oChapter.Time.TotalSeconds * fpsIn;
            return new Chapter() { Name = oChapter.Name, Time = new TimeSpan((long)Math.Round(frames / fpsOut * TimeSpan.TicksPerSecond)) };
        }
    }
    public class ChapterApi
    {
        public string Title { get; set; }
        public string SourceFilePath { get; set; }
        public string SourceType { get; set; }
        public double FramesPerSecond { get; set; }
        public int TitleNumber { get; set; }
        public int PGCNumber { get; set; }
        public int AngleNumber { get; set; }
        public List<Chapter> Chapters { get; set; }

        [XmlIgnore]
        public TimeSpan Duration { get; set; }

        // XmlSerializer does not support TimeSpan, so use this property for serialization instead
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public long DurationTicks
        {
            get { return Duration.Ticks; }
            set { Duration = new TimeSpan(value); }
        }

        public static bool ChapterFormatCheck(string chapterpath)
        {
            ChapterApi chapter = new ChapterApi();
            return chapter.LoadText(chapterpath);
        }

        public ChapterApi()
        {
            ResetChapterInfo();
        }

        public ChapterApi(ChapterApi oOther)
        {
            Title = oOther.Title;
            SourceFilePath = oOther.SourceFilePath;
            SourceType = oOther.SourceType;
            FramesPerSecond = oOther.FramesPerSecond;
            TitleNumber = oOther.TitleNumber;
            PGCNumber = oOther.PGCNumber;
            AngleNumber = oOther.AngleNumber;
            Chapters = new List<Chapter>();
            oOther.Chapters.ForEach((item) =>
            {
                Chapters.Add(new Chapter(item));
            });
        }

        private void ResetChapterInfo()
        {
            Title = string.Empty;
            SourceFilePath = string.Empty;
            SourceType = string.Empty;
            FramesPerSecond = 0;
            TitleNumber = 0;
            PGCNumber = 0;
            AngleNumber = 0;
            Chapters = new List<Chapter>();
        }

        public override string ToString()
        {
            string strResult = string.Empty;
            string strTitle = (String.IsNullOrEmpty(Title) ? Path.GetFileName(SourceFilePath) : Title);
            if (SourceType.Equals("DVD"))
                strTitle += "  -  PGC " + PGCNumber.ToString("D2");
            if (Chapters.Count != 1)
                strResult = string.Format("{0}  -  {1}  -  [{2} Chapters]", strTitle, string.Format("{0:00}:{1:00}:{2:00}.{3:000}", System.Math.Floor(Duration.TotalHours), Duration.Minutes, Duration.Seconds, Duration.Milliseconds), Chapters.Count);
            else
                strResult = string.Format("{0}  -  {1}  -  [{2} Chapter]", strTitle, string.Format("{0:00}:{1:00}:{2:00}.{3:000}", System.Math.Floor(Duration.TotalHours), Duration.Minutes, Duration.Seconds, Duration.Milliseconds), Chapters.Count);
            if (AngleNumber > 0)
                strResult += "  -  Angle " + AngleNumber;
            return strResult;
        }

        [XmlIgnore]
        public bool HasChapters
        {
            get { return (Chapters.Count > 0); }
        }

        public bool LoadFile(string strFileName)
        {
            ResetChapterInfo();

            if (!File.Exists(strFileName))
                return false;

            // try to get the chapter information from plain chapter files
            FileInfo oFileInfo = new FileInfo(strFileName);
            if (oFileInfo.Length < 20971520) // max 20 MB files are supported to avoid reading large files
            {
                if (LoadText(strFileName) || LoadText2(strFileName) || LoadXML(strFileName))
                    return true;
            }

            // now try mediainfo
            //MediaInfoFile oInfo = new MediaInfoFile(strFileName);
            //if (!oInfo.HasChapters)
            //    return false;

            //Chapters = oInfo.ChapterInfo.Chapters;
            //SourceFilePath = oInfo.ChapterInfo.SourceFilePath;
            //SourceType = oInfo.ChapterInfo.SourceType;
            //FramesPerSecond = oInfo.ChapterInfo.FramesPerSecond;
            //Title = oInfo.ChapterInfo.Title;
            //Duration = oInfo.ChapterInfo.Duration;
            return true;
        }

        #region helper functions
        public void ChangeFps(double fps)
        {
            if (FramesPerSecond == 0 || FramesPerSecond == fps)
            {
                FramesPerSecond = fps;
                return;
            }

            for (int i = 0; i < Chapters.Count; i++)
                Chapters[i] = Chapter.ChangeChapterFPS(Chapters[i], FramesPerSecond, fps);

            double totalFrames = Duration.TotalSeconds * FramesPerSecond;
            Duration = new TimeSpan((long)Math.Round((totalFrames / fps) * TimeSpan.TicksPerSecond));
            FramesPerSecond = fps;
        }

        /// <summary>
        /// gets Timeline for tsMuxeR
        /// </summary>
        /// <returns>chapters Timeline as string</returns>
        public string GetChapterTimeLine()
        {
            string strTimeLine = string.Empty;

            foreach (Chapter oChapter in Chapters)
                strTimeLine += oChapter.Time.ToString().Substring(0, 12) + ";";

            if (strTimeLine.EndsWith(";"))
                strTimeLine = strTimeLine.Substring(0, strTimeLine.Length - 1);

            return strTimeLine;
        }
        #endregion

        #region load file
        private bool LoadText(string strFileName)
        {
            try
            {
                int num = 0;
                TimeSpan ts = new TimeSpan(0);
                string time = String.Empty;
                string name = String.Empty;
                bool onTime = true;
                string[] lines = File.ReadAllLines(strFileName, Encoding.Default);
                foreach (string line in lines)
                {
                    if (line == "")
                    {
                        continue;
                    }
                    if (onTime)
                    {
                        num++;
                        //read time
                        time = line.Replace("CHAPTER" + num.ToString("00") + "=", "");
                        ts = TimeSpan.Parse(time);
                    }
                    else
                    {
                        //read name
                        if (!line.StartsWith("CHAPTER" + num.ToString("00") + "NAME="))
                        {
                            throw new Exception();
                        }
                        name = line.Replace("CHAPTER" + num.ToString("00") + "NAME=", "");

                        //add it to list
                        Chapters.Add(new Chapter() { Name = name, Time = ts });
                    }
                    onTime = !onTime;
                }

                SourceFilePath = strFileName;
                Title = Path.GetFileNameWithoutExtension(strFileName);
                if (Chapters.Count > 0)
                    Duration = Chapters[Chapters.Count - 1].Time;
            }
            catch (Exception)
            {
                Chapters.Clear();
                MessageBoxApi.Show(LanguageApi.FindRes("chapterFormatErrorTipsDesc"), LanguageApi.FindRes("error"));
                return false;
            }

            return Chapters.Count != 0;
        }

        private bool LoadText2(string strFileName)
        {
            // 00:00:00.000 Prologue
            // 00:00:14.000 Opening

            try
            {
                foreach (string line in File.ReadAllLines(strFileName, Encoding.Default))
                {
                    int iPos = line.IndexOf(' ');
                    if (iPos <= 0)
                        continue;

                    string chapterTime = line.Split(' ')[0];
                    TimeSpan chapterSpan;
                    if (!TimeSpan.TryParse(chapterTime, out chapterSpan))
                        continue;

                    Chapters.Add(new Chapter() { Name = line.Substring(iPos + 1), Time = chapterSpan });
                }

                SourceFilePath = strFileName;
                Title = Path.GetFileNameWithoutExtension(strFileName);
                if (Chapters.Count > 0)
                    Duration = Chapters[Chapters.Count - 1].Time;
            }
            catch (Exception)
            {
                Chapters.Clear();
                return false;
            }

            return Chapters.Count != 0;
        }

        private bool LoadXML(string strFileName)
        {
            try
            {
                XmlDocument oChap = new XmlDocument();
                oChap.Load(strFileName);

                foreach (XmlNode oFirstNode in oChap.ChildNodes)
                {
                    if (!oFirstNode.Name.ToLowerInvariant().Equals("chapters"))
                        continue;

                    foreach (XmlNode oSecondNode in oFirstNode.ChildNodes)
                    {
                        if (!oSecondNode.Name.ToLowerInvariant().Equals("editionentry"))
                            continue;

                        foreach (XmlNode oThirdNode in oSecondNode.ChildNodes)
                        {
                            if (!oThirdNode.Name.ToLowerInvariant().Equals("chapteratom"))
                                continue;

                            Chapter oChapter = new Chapter();
                            foreach (XmlNode oChapterNode in oThirdNode.ChildNodes)
                            {
                                if (oChapterNode.Name.ToLowerInvariant().Equals("chaptertimestart"))
                                {
                                    oChapter.SetTimeBasedOnString(oChapterNode.InnerText);
                                }
                                else if (oChapterNode.Name.ToLowerInvariant().Equals("chapterdisplay"))
                                {
                                    foreach (XmlNode oChapterString in oChapterNode.ChildNodes)
                                    {
                                        if (oChapterString.Name.ToLowerInvariant().Equals("chapterstring"))
                                        {
                                            oChapter.Name = oChapterString.InnerText;
                                        }
                                    }
                                }
                            }
                            Chapters.Add(oChapter);
                        }
                        break; // avoid multiple editions
                    }
                }

                SourceFilePath = strFileName;
                Title = Path.GetFileNameWithoutExtension(strFileName);
                if (Chapters.Count > 0)
                    Duration = Chapters[Chapters.Count - 1].Time;
            }
            catch (Exception)
            {
                Chapters.Clear();
                return false;
            }

            return Chapters.Count != 0;
        }
        #endregion

        #region save file
        public bool SaveText(string strFileName)
        {
            try
            {
                List<string> lines = new List<string>();
                int i = 0;
                foreach (Chapter c in Chapters)
                {
                    i++;
                    if (c.Time.ToString().Length == 8)
                        lines.Add("CHAPTER" + i.ToString("00") + "=" + c.Time.ToString() + ".000"); // better formating
                    else if (c.Time.ToString().Length > 12)
                        lines.Add("CHAPTER" + i.ToString("00") + "=" + c.Time.ToString().Substring(0, 12)); // remove some duration length too long
                    else
                        lines.Add("CHAPTER" + i.ToString("00") + "=" + c.Time.ToString());
                    lines.Add("CHAPTER" + i.ToString("00") + "NAME=" + c.Name);
                }
                File.WriteAllLines(strFileName, lines.ToArray(), Encoding.UTF8);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool SaveQpfile(string strFileName)
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (Chapter c in Chapters)
                    lines.Add(string.Format("{0} K", (long)Math.Round(c.Time.TotalSeconds * FramesPerSecond)));
                File.WriteAllLines(strFileName, lines.ToArray());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool SaveXml(string filename)
        {
            try
            {
                Random rndb = new Random();
                XmlTextWriter xmlchap = new XmlTextWriter(filename, Encoding.UTF8);
                xmlchap.Formatting = Formatting.Indented;
                xmlchap.WriteStartDocument();
                xmlchap.WriteComment("<!DOCTYPE Tags SYSTEM " + "\"" + "matroskatags.dtd" + "\"" + ">");
                xmlchap.WriteStartElement("Chapters");
                xmlchap.WriteStartElement("EditionEntry");
                xmlchap.WriteElementString("EditionFlagHidden", "0");
                xmlchap.WriteElementString("EditionFlagDefault", "0");
                xmlchap.WriteElementString("EditionUID", Convert.ToString(rndb.Next(1, Int32.MaxValue)));
                foreach (Chapter c in Chapters)
                {
                    xmlchap.WriteStartElement("ChapterAtom");
                    xmlchap.WriteStartElement("ChapterDisplay");
                    xmlchap.WriteElementString("ChapterString", c.Name);
                    xmlchap.WriteElementString("ChapterLanguage", "und");
                    xmlchap.WriteEndElement();
                    xmlchap.WriteElementString("ChapterUID", Convert.ToString(rndb.Next(1, Int32.MaxValue)));
                    if (c.Time.ToString().Length == 8)
                        xmlchap.WriteElementString("ChapterTimeStart", c.Time.ToString() + ".0000000");
                    else
                        xmlchap.WriteElementString("ChapterTimeStart", c.Time.ToString());
                    xmlchap.WriteElementString("ChapterFlagHidden", "0");
                    xmlchap.WriteElementString("ChapterFlagEnabled", "1");
                    xmlchap.WriteEndElement();
                }
                xmlchap.WriteEndElement();
                xmlchap.WriteEndElement();
                xmlchap.Flush();
                xmlchap.Close();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Creates Apple Style Chapters XML File
        /// </summary>
        /// <param name="strFileName">output file name</inFile>
        public bool SaveAppleXML(string strFileName)
        {
            try
            {
                XmlTextWriter xmlchap = new XmlTextWriter(strFileName, Encoding.UTF8);
                xmlchap.Formatting = Formatting.Indented;

                xmlchap.WriteStartDocument();
                xmlchap.WriteComment("GPAC 3GPP Text Stream");
                xmlchap.WriteStartElement("TextStream");
                xmlchap.WriteAttributeString("version", "1.1");
                xmlchap.WriteStartElement("TextStreamHeader");
                xmlchap.WriteStartElement("TextSampleDescription");
                xmlchap.WriteEndElement();
                xmlchap.WriteEndElement();

                foreach (Chapter c in Chapters)
                {
                    xmlchap.WriteStartElement("TextSample");
                    if (c.Time.ToString().Length == 8)
                        xmlchap.WriteAttributeString("sampleTime", c.Time.ToString() + ".000");
                    else
                        xmlchap.WriteAttributeString("sampleTime", c.Time.ToString().Substring(0, 12));
                    xmlchap.WriteAttributeString("text", c.Name.ToString());
                    xmlchap.WriteEndElement();
                }

                // add a final dummy chapter element to prevent incorrect length reporting
                Chapter last = Chapters[Chapters.Count - 1];
                xmlchap.WriteStartElement("TextSample");
                xmlchap.WriteAttributeString("sampleTime", (TimeSpan.Parse(last.Time.ToString()) + new TimeSpan(0, 0, 0, 0, 500)).ToString(@"hh\:mm\:ss\.fff"));
                xmlchap.WriteAttributeString("xml:space", "preserve");
                xmlchap.WriteEndElement();

                xmlchap.WriteEndElement(); // close TextStream
                xmlchap.Flush();
                xmlchap.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
    }
}