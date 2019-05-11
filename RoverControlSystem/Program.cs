using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;



namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // Default values - Names
        static readonly UpdateFrequency defaultUpdateFrequency_ = UpdateFrequency.Update10;
        static readonly UpdateType defaultUpdateType_ = UpdateType.Update10;
        static readonly string defaultBreakLightsGroupName_ = "[COH-101] Lights Break"; // group name
        static readonly string defaultConnectorLightsGroupName_ = "[COH-101] Lights Connector"; // group name

        // Default values - States
        static readonly bool defaultUseFastMode_ = false;
        //static readonly bool defaultUseAutoFastMode_ = true;


        const string rcsVersion_ = "0.50 Alpha";


        #region Visualization
        List<Surface> surfaces_ = new List<Surface>();
        Dictionary<Module.ModuleState, Color> moduleStateColor_;


        private void UpdateLCDPanels()
        {
            foreach (var surface in surfaces_)
            {
                if (surface.processUpdate())
                    return;
            }
        }


        private void ShutdownLCDPanels()
        {
            foreach (var surface in surfaces_)
                surface.shutdown();
        }


        class Surface
        {
            // default values
            public static readonly string defaultUITextFont_ = "Monospace";
            public static readonly float defaultUIPadding_ = 10f;
            public static readonly TimeSpan defaultLCDUpdateInterval_ = new TimeSpan(0, 0, 1); // 1sec

            // Default values - Colors
            public static readonly Color defaultSurfaceBackgroundColor_ = new Color(0, 38, 121);
            public static readonly Color defaultUITextColor_ = Color.Azure;
            public static readonly Color defaultLogoBackgroundColor_ = Color.AntiqueWhite;
            public static readonly Color defaultLogoForgroundColor_ = Color.Azure;

            public static readonly Color defaultStateColorInvalid_ = Color.Red;
            public static readonly Color defaultStateColorBootup_ = Color.DarkBlue;
            public static readonly Color defaultStateColorStopped_ = Color.OrangeRed;
            public static readonly Color defaultStateColorResume_ = Color.Orange;
            public static readonly Color defaultStateColorRunning_ = Color.Green;
            public static readonly Color defaultStateColorFailure_ = Color.Black;

            // basic values
            Program app_ = null;
            IMyTextSurface surface_ = null;
            int index_ = 0;
            string configSection_ = string.Empty;
            DateTime lastUpdate_ = DateTime.Now;

            // configurations
            Color surfaceBackgroundColor_ = defaultSurfaceBackgroundColor_;
            Color logoBackgroundColor_ = defaultLogoBackgroundColor_;
            Color logoForgroundColor_ = defaultLogoForgroundColor_;
            Color UITextColor_ = defaultUITextColor_;
            string UITextFont_ = defaultUITextFont_;
            Vector2 UIFontSize_ = new Vector2();
            float UIPadding_ = defaultUIPadding_;

            // bar colors
            Segment[] barColorsTopBetter_ = { new Segment(0.15f, Color.Red), new Segment(0.4f, Color.Yellow), new Segment(1.0f, Color.Green) };
            Segment[] barColorsBtmBetter_ = { new Segment(0.6f, Color.Green), new Segment(0.85f, Color.Yellow), new Segment(1.0f, Color.Red) };
            Color barBackgroundColor_ = defaultSurfaceBackgroundColor_;


            public Surface(Program app, IMyTextSurface surface, int index, string configSection)
            {
                app_ = app;
                surface_ = surface;
                index_ = index;
                configSection_ = configSection;

                loadConfiguration();
            }


            public void loadConfiguration()
            {
                app_.Config.getValue(configSection_, "Font", out UITextFont_, defaultUITextFont_);
                app_.Config.getValue(configSection_, "FontColor", out UITextColor_, defaultUITextColor_);
                app_.Config.getValue(configSection_, "UIPadding", out UIPadding_, defaultUIPadding_);
                app_.Config.getValue(configSection_, "BackgroundColor", out surfaceBackgroundColor_, defaultSurfaceBackgroundColor_);
                app_.Config.getValue(configSection_, "BackgroundBarColor", out barBackgroundColor_, defaultSurfaceBackgroundColor_);
            }


            public void saveConfiguration()
            {
                app_.Config.setValue(configSection_, "Font", UITextFont_);
                app_.Config.setValue(configSection_, "FontColor", UITextColor_);
                app_.Config.setValue(configSection_, "UIPadding", UIPadding_);
                app_.Config.setValue(configSection_, "BackgroundColor", surfaceBackgroundColor_);
                app_.Config.setValue(configSection_, "BackgroundBarColor", barBackgroundColor_);
            }


            public void shutdown()
            {
                // print shutdown message
            }


            public bool processUpdate()
            {
                // time to update?
                DateTime curTime = DateTime.Now;
                if ((curTime - lastUpdate_) < defaultLCDUpdateInterval_)
                    return false;

                lastUpdate_ = curTime;

                // setup surface
                surface_.WriteText(string.Empty);
                surface_.ContentType = VRage.Game.GUI.TextPanel.ContentType.SCRIPT;
                surface_.Script = string.Empty;
                surface_.ScriptBackgroundColor = surfaceBackgroundColor_;
                surface_.ScriptForegroundColor = Color.Azure;

                UIFontSize_ = surface_.MeasureStringInPixels(new StringBuilder("M"), UITextFont_, 1.0f);

                // draw
                using (var frame = surface_.DrawFrame())
                {
                    // draw overlay
                    // header line
                    MySprite headerline = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(256f, 73f),
                        new Vector2(512f, 5f));
                    frame.Add(headerline);

                    // footer line
                    MySprite footerline = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(256f, 512f - 54f),
                        new Vector2(512f, 5f));
                    frame.Add(footerline);

                    // split lines
                    Vector2 splitLineSize = new Vector2(5f, 374f);
                    MySprite splitLine1 = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(350f, 256f + 5f), splitLineSize);
                    MySprite splitLine2 = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(167f, 256f + 5f), splitLineSize);
                    frame.Add(splitLine1);
                    frame.Add(splitLine2);

                    // draw informations
                    DrawHeader(frame, new Vector2(1f));
                    DrawModuleStateIndicators(frame, new Vector2(0f, 73f), new Vector2(167f, 385f));
                    DrawEnergieInformations(frame, new Vector2(350f, 73f), new Vector2(162f, 385f));
                    DrawStatusInformations(frame, new Vector2(167f, 73f), new Vector2(183f, 200f));
                    DrawFooter(frame, new Vector2(0f, 450f), new Vector2(512f, 54f));
                }

                return true;
            }


            #region Single Drawing Methods
            private void DrawHeader(MySpriteDrawFrame frame, Vector2 scale)
            {
                // logo
                MySprite logoIcon1 = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(40, 32), new Vector2(60, 60), logoBackgroundColor_);
                MySprite logoIcon2 = new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(50, 32), new Vector2(50, 30), surfaceBackgroundColor_);
                MySprite logoIcon3 = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(88, 50), new Vector2(125, 10), logoForgroundColor_);
                frame.Add(logoIcon1);
                frame.Add(logoIcon2);
                frame.Add(logoIcon3);
                MySprite logoText = MySprite.CreateText("RCS", "Monospace", new Color(0, 10, 85), 2f);
                logoText.Position = new Vector2(40, 4);
                logoText.Alignment = TextAlignment.LEFT;
                frame.Add(logoText);

                // version
                MySprite version = MySprite.CreateText(rcsVersion_, UITextFont_, UITextColor_, 0.9f, TextAlignment.LEFT);
                version.Position = new Vector2(160f, 40f);
                frame.Add(version);
            }


            private void DrawFooter(MySpriteDrawFrame frame, Vector2 position, Vector2 size)
            {
                // draw clock
                string clockText = DateTime.Now.ToShortTimeString() + " " + DateTime.Now.ToShortDateString();
                float scale = Math.Min((size.Y - (UIPadding_ * 2)) / UIFontSize_.Y, (size.X - (UIPadding_ * 2)) / (UIFontSize_.X * clockText.Length));

                MySprite clock = MySprite.CreateText(clockText, UITextFont_, UITextColor_, scale);
                clock.Position = new Vector2(position.X + size.X * 0.5f, position.Y + UIPadding_ + (size.Y - (UIFontSize_.Y * scale)) * 0.5f);
                frame.Add(clock);
            }


            private void DrawEnergieInformations(MySpriteDrawFrame frame, Vector2 position, Vector2 size)
            {
                float UIPadding3 = UIPadding_ * 3f;
                float UIPadding4 = UIPadding_ * 4f;
                Vector2 iconSize = new Vector2((size.X - UIPadding4) / 3f);

                EnergieManager em = app_.findModuleById("EM") as EnergieManager;
                InventoryManager im = app_.findModuleById("IM") as InventoryManager;

                // general bar values
                Vector2 barSize = new Vector2((size.X - UIPadding4) / 3f, size.Y - iconSize.Y - UIPadding3);
                float barPosY = position.Y + 10f + (barSize.Y * 0.5f);

                // battery bars
                DrawProgressBar(frame, em.EnergieInUsage, new Vector2(position.X + barSize.X * 2.5f + UIPadding3, barPosY),
                    barSize, barColorsBtmBetter_, barBackgroundColor_, true);
                DrawProgressBar(frame, em.BatteryPowerLeft, new Vector2(position.X + barSize.X * 1.5f + (UIPadding_ * 2f), barPosY),
                    barSize, barColorsTopBetter_, barBackgroundColor_, true);

                // Power generator bars
                DrawProgressBar(frame, (float)im.HydrogenFillRatio, new Vector2(position.X + barSize.X * 0.5f + UIPadding_, barPosY),
                    barSize, barColorsTopBetter_, barBackgroundColor_, true);

                // draw icons
                float iconPosY = position.Y + (iconSize.Y * 0.5f) + (UIPadding_ * 2f) + barSize.Y;

                MySprite energy1 = new MySprite(SpriteType.TEXTURE, "IconEnergy", new Vector2(position.X + iconSize.X * 2.5f + UIPadding3, iconPosY), iconSize);
                MySprite energy2 = new MySprite(SpriteType.TEXTURE, "IconEnergy", new Vector2(position.X + iconSize.X * 1.5f + (UIPadding_ * 2f), iconPosY), iconSize);
                MySprite h2 = new MySprite(SpriteType.TEXTURE, "IconHydrogen", new Vector2(position.X + iconSize.X * 0.5f + UIPadding_, iconPosY), iconSize);
                frame.Add(energy1);
                frame.Add(energy2);
                frame.Add(h2);
            }


            private void DrawModuleStateIndicators(MySpriteDrawFrame frame, Vector2 position, Vector2 size)
            {
                float lineHeight = (size.Y - ((app_.modules_.Count + 1) * UIPadding_)) / app_.modules_.Count;
                Vector2 fontScaleVector = new Vector2(((size.X * 0.6f) - (UIPadding_ * 3f)) / (UIFontSize_.X * 3), lineHeight / UIFontSize_.Y);
                float fontScale = Math.Min(fontScaleVector.X, fontScaleVector.Y);

                if (fontScale < 0.9f)
                {
                    // Draw text only
                    float offsetY = UIPadding_;
                    fontScale = Math.Min((size.X - (UIPadding_ * 2f)) / (UIFontSize_.X * 3), lineHeight / UIFontSize_.Y);

                    // ID Text
                    foreach (var module in app_.modules_)
                    {
                        MySprite id = MySprite.CreateText(module.Id, UITextFont_, app_.moduleStateColor_[module.State], fontScale, TextAlignment.LEFT);
                        id.Position = new Vector2(position.X + UIPadding_, position.Y + offsetY);
                        frame.Add(id);

                        offsetY += lineHeight + UIPadding_;
                    }
                }
                else
                {
                    float offsetY = UIPadding_ + ((lineHeight - (UIFontSize_.Y * fontScale)) * 0.5f);
                    float offsetX = (UIFontSize_.X * fontScale * 3f) + (UIPadding_ * 2f);
                    Vector2 boxSize = new Vector2(size.X - offsetX - UIPadding_, UIFontSize_.Y * fontScale);

                    // Draw with indicator box
                    foreach (var module in app_.modules_)
                    {
                        // ID Text
                        MySprite id = MySprite.CreateText(module.Id, UITextFont_, UITextColor_, fontScale, TextAlignment.LEFT);
                        id.Position = new Vector2(position.X + UIPadding_, position.Y + offsetY);
                        frame.Add(id);

                        // Indicator
                        MySprite indicator = new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(offsetX, position.Y + offsetY + (boxSize.Y * 0.5f)),
                            boxSize, app_.moduleStateColor_[module.State], alignment: TextAlignment.LEFT);
                        frame.Add(indicator);

                        offsetY += lineHeight + UIPadding_;
                    }
                }
            }


            private void DrawStatusInformations(MySpriteDrawFrame frame, Vector2 position, Vector2 size)
            {
                float offsetX = position.X + (size.X * 0.5f);
                float offsetY = position.Y + UIPadding_;

                const int lineCount = 5;
                const int charCount = 10;

                float lineHeight = (size.Y - ((lineCount + 1) * UIPadding_)) / lineCount;
                float fontScale = Math.Min((size.X - (UIPadding_ * 2f)) / (UIFontSize_.X * charCount), lineHeight / UIFontSize_.Y);

                // docking state
                DockingController dc = app_.findModuleById("DC") as DockingController;
                if (dc != null)
                {
                    // connect state
                    Color csSColor = dc.IsConnected ? Color.Green : (dc.IsReadyToConnect ? Color.Yellow : UITextColor_);
                    MySprite csText = MySprite.CreateText("Connected", UITextFont_, csSColor, fontScale);
                    csText.Position = new Vector2(offsetX, offsetY);
                    frame.Add(csText);
                    offsetY += lineHeight + UIPadding_;

                    // home base
                    Color hbColor = dc.IsHomeBase ? Color.Green : (dc.IsConnected || dc.IsReadyToConnect ? Color.Red : UITextColor_);
                    MySprite hbText = MySprite.CreateText("Home Base", UITextFont_, hbColor, fontScale);
                    hbText.Position = new Vector2(offsetX, offsetY);
                    frame.Add(hbText);
                    offsetY += lineHeight + UIPadding_;
                }

                // RCS states
                MainRCSSystem rcs = app_.findModuleById("RCS") as MainRCSSystem;
                if (rcs != null)
                {
                    Color color = rcs.IsParking ? Color.Red : UITextColor_;
                    MySprite text = MySprite.CreateText("Parking", UITextFont_, color, fontScale);
                    text.Position = new Vector2(offsetX, offsetY);
                    frame.Add(text);
                    offsetY += lineHeight + UIPadding_;
                }
            }
            #endregion // Single Drawing Methods


            #region Bar Tools
            private struct Segment
            {
                public Segment(float value, Color color)
                {
                    value_ = value;
                    color_ = color;
                }

                public float value_; // this is the max value. The start value depends on the previews segment.
                public Color color_;
            }


            private void DrawProgressBar(MySpriteDrawFrame frame, float fillRatio, Vector2 position, Vector2 size,
                Segment[] segments, Color backgroundColor, bool vertical = false)
            {
                // draw background
                if (backgroundColor.A > 0)
                {
                    MySprite background = new MySprite(SpriteType.TEXTURE, "SquareSimple", position, size, backgroundColor);
                    frame.Add(background);
                }

                // draw bar
                float startRatio = 0f;

                for (int s = 0; s < segments.Length && startRatio <= fillRatio; ++s)
                {
                    float ratio = Math.Min(segments[s].value_, fillRatio) - startRatio;
                    Vector2 barSize;
                    Vector2 barPosition;

                    if (vertical)
                    {
                        barSize = new Vector2(size.X, size.Y * ratio);
                        barPosition = new Vector2(position.X, position.Y + ((size.Y - barSize.Y) * 0.5f) - (size.Y * startRatio));
                    }
                    else
                    {
                        barSize = new Vector2(size.X * ratio, size.Y);
                        barPosition = new Vector2(position.X - ((size.X - barSize.X) * 0.5f) + (size.X * startRatio), position.Y);
                    }


                    MySprite bar = new MySprite(SpriteType.TEXTURE, "SquareSimple", barPosition, barSize, segments[s].color_);
                    frame.Add(bar);

                    startRatio += ratio;
                }
            }
            #endregion // Bar Tools
        }
        #endregion // Visualization


        #region Commandline
        public bool processCommandLine(string args)
        {
            MyCommandLine parser = new MyCommandLine();
            if (parser.TryParse(args))
            {
                if (parser.Switch("reboot"))
                {
                    stateHandler_ = handleInvalidateState;
                }

                return true;
            }
            return false;
        }
        #endregion


        #region Configuration
        private Configuration config_ = null;
        public Configuration Config
        {
            get
            {
                return config_;
            }
        }


        private bool loadConfiguration()
        {
            config_.invalidate();
            if (!config_.read())
                return false;

            // read system configuration
            config_.getValue("System", "FastMode", out useFastMode_, defaultUseFastMode_);

            return true;
        }


        private void saveConfiguration()
        {
            foreach (var module in modules_)
                module.onSaveConfiguration();

            foreach (var surface in surfaces_)
                surface.saveConfiguration();

            // save system settings
            config_.setValue("System", "FastMode", useFastMode_);

            config_.write();
        }


        public class Configuration
        {
            Program app_ = null;
            MyIni config_ = new MyIni();

            public delegate bool SearchKeyCallback(MyIniKey key);
            public delegate bool SearchSectionCallback(string section);


            public Configuration(Program app)
            {
                app_ = app;
            }


            public bool read()
            {
                if (!config_.TryParse(app_.Me.CustomData))
                    return false;

                // read all sections
                List<string> sections = new List<string>();
                config_.GetSections(sections);
                foreach(var section in sections)
                {
                    List<MyIniKey> keys = new List<MyIniKey>();
                    config_.GetKeys(section, keys);
                    foreach(var key in keys)
                    {
                        var value = config_.Get(key);
                    }
                }

                return true;
            }


            public void write()
            {
                app_.Me.CustomData = config_.ToString();
            }


            public void invalidate()
            {
                config_.Clear();
            }


            #region Tools
            public bool sectionExists(string section)
            {
                return config_.ContainsSection(section);
            }


            public bool keyExists(string section, string key)
            {
                return config_.ContainsKey(section, key);
            }


            public bool search(string section, SearchKeyCallback callback, out string key)
            {
                key = string.Empty;
                List<MyIniKey> keys = new List<MyIniKey>();
                config_.GetKeys(section, keys);
                foreach (MyIniKey INIKey in keys)
                {
                    if (callback(INIKey))
                    {
                        key = INIKey.Name;
                        return true;
                    }
                }

                return false;
            }


            public void search(string section, SearchKeyCallback callback, out List<string> keys)
            {
                keys = new List<string>();
                List<MyIniKey> INIKeys = new List<MyIniKey>();
                config_.GetKeys(section, INIKeys);
                foreach (MyIniKey INIKey in INIKeys)
                {
                    if (callback(INIKey))
                        keys.Add(INIKey.Name);
                }
            }


            public bool search(SearchSectionCallback callback, out string section)
            {
                section = string.Empty;
                List<string> sections = new List<string>();
                config_.GetSections(sections);
                foreach(string INISection in sections)
                {
                    if (callback(INISection))
                        return true;
                }

                return false;
            }


            public void search(SearchSectionCallback callback, out List<string> sections)
            {
                sections = new List<string>();
                List<string> INISections = new List<string>();
                config_.GetSections(INISections);
                foreach (string INISection in INISections)
                {
                    if (callback(INISection))
                        sections.Add(INISection);
                }
            }
            #endregion // Tools


            #region Register Keys
            public void registerKey(string section, string key, bool value, string comment = "")
            {
                // write default value
                if (!keyExists(section, key))
                    setValue(section, key, value, comment);
            }


            public void registerKey(string section, string key, int value, string comment = "")
            {
                // write default value
                if (!keyExists(section, key))
                    setValue(section, key, value, comment);
            }


            public void registerKey(string section, string key, float value, string comment = "")
            {
                // write default value
                if (!keyExists(section, key))
                    setValue(section, key, value, comment);
            }


            public void registerKey(string section, string key, string value, string comment = "")
            {
                // write default value
                if (!keyExists(section, key))
                    setValue(section, key, value, comment);
            }


            public void registerKey(string section, string key, Vector2 value, string comment = "")
            {
                // write default value
                if (!keyExists(section, key))
                    setValue(section, key, value, comment);
            }


            public void registerKey(string section, string key, Vector3 value, string comment = "")
            {
                // write default value
                if (!keyExists(section, key))
                    setValue(section, key, value, comment);
            }


            public void registerKey(string section, string key, Color value, string comment = "")
            {
                // write default value
                if (!keyExists(section, key))
                    setValue(section, key, value, comment);
            }
            #endregion // Register Keys


            #region Set Values
            public void setValue(string section, string key, bool value, string comment = "")
            {
                config_.Set(section, key, value);
                config_.SetComment(section, key, comment);
            }


            public void setValue(string section, string key, int value, string comment = "")
            {
                config_.Set(section, key, value);
                config_.SetComment(section, key, comment);
            }


            public void setValue(string section, string key, float value, string comment = "")
            {
                config_.Set(section, key, value);
                config_.SetComment(section, key, comment);
            }


            public void setValue(string section, string key, string value, string comment = "")
            {
                config_.Set(section, key, value);
                config_.SetComment(section, key, comment);
            }


            public void setValue(string section, string key, Vector2 value, string comment = "")
            {
                config_.Set(section, key, string.Format("{0},{1}", value.X, value.Y));
                config_.SetComment(section, key, comment);
            }


            public void setValue(string section, string key, Vector3 value, string comment = "")
            {
                config_.Set(section, key, string.Format("{0},{1},{2}", value.X, value.Y, value.Z));
                config_.SetComment(section, key, comment);
            }


            public void setValue(string section, string key, Color value, string comment = "")
            {
                config_.Set(section, key, string.Format("{0},{1},{2},{3}", value.R, value.G, value.B, value.A));
                config_.SetComment(section, key, comment);
            }
            #endregion


            #region Get Values
            public void getValue(string section, string key, out bool value, bool defaultValue = false)
            {
                value = config_.Get(section, key).ToBoolean(defaultValue);
            }


            public void getValue(string section, string key, out int value, int defaultValue = 0)
            {
                value = config_.Get(section, key).ToInt32(defaultValue);
            }


            public void getValue(string section, string key, out float value, float defaultValue = 0f)
            {
                value = (float)config_.Get(section, key).ToDouble(defaultValue);
            }


            public void getValue(string section, string key, out string value, string defaultValue = "")
            {
                value = config_.Get(section, key).ToString(defaultValue);
            }


            public void getValue(string section, string key, out Vector2 value, Vector2 defaultValue)
            {
                string raw = config_.Get(section, key).ToString();
                string[] parts = raw.Split(',');

                if (parts.Length == 2)
                {
                    float x = 0f;
                    float y = 0f;

                    float.TryParse(parts[0].Trim(), out x);
                    float.TryParse(parts[1].Trim(), out y);

                    value = new Vector2(x, y);
                }
                else
                    value = defaultValue;
            }


            public void getValue(string section, string key, out Vector3 value, Vector3 defaultValue)
            {
                string raw = config_.Get(section, key).ToString();
                string[] parts = raw.Split(',');

                if (parts.Length == 3)
                {
                    float x = 0f;
                    float y = 0f;
                    float z = 0f;

                    float.TryParse(parts[0].Trim(), out x);
                    float.TryParse(parts[1].Trim(), out y);
                    float.TryParse(parts[2].Trim(), out z);

                    value = new Vector3(x, y, z);
                }
                else
                    value = defaultValue;
            }


            public void getValue(string section, string key, out Color value, Color defaultValue)
            {
                string raw = config_.Get(section, key).ToString();
                string[] parts = raw.Split(',');

                if (parts.Length >= 3)
                {
                    int r = 0;
                    int g = 0;
                    int b = 0;
                    int a = 0;

                    int.TryParse(parts[0].Trim(), out r);
                    int.TryParse(parts[1].Trim(), out g);
                    int.TryParse(parts[2].Trim(), out b);

                    if (!int.TryParse(parts[3].Trim(), out a))
                        a = 255;

                    value = new Color(
                        MathHelper.Clamp(r, 0, 255),
                        MathHelper.Clamp(g, 0, 255), 
                        MathHelper.Clamp(b, 0, 255), 
                        MathHelper.Clamp(a, 0, 255));
                }
                else
                    value = defaultValue;
            }
            #endregion // Get Values
        }
        #endregion // Configuration


        #region RCS System State
        int nextModulePosition_ = 0;
        bool useFastMode_ = defaultUseFastMode_;
        Action<UpdateType> stateHandler_ = null;


        #region System State Handler
        private void handleInvalidateState(UpdateType updateType)
        {
            foreach (var module in modules_)
            {
                module.onInvalidate();
            }

            // regenerate configuration
            if (!loadConfiguration())
            {
                // kernel panic ^^
                stateHandler_ = handlePanicState;
                return;
            }

            // clear all surface
            surfaces_.Clear();

            // scan all blocks
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(null, (block) =>
            {
                //if (!block.IsSameConstructAs(block))
                if (Me.CubeGrid != block.CubeGrid)
                    return false;

                // search for LCD Panels
                IMyTextSurface surface = block as IMyTextSurface;
                if (surface != null && config_.sectionExists($"LCD:({block.CustomName}):0"))
                {
                    surfaces_.Add(new Surface(this, surface, 0, $"LCD:({block.CustomName}):0"));
                    return false;
                }

                // Search for display inside a cockpit that is used
                IMyCockpit cockpit = block as IMyCockpit;
                if (cockpit != null)
                {
                    for (int i = 0; i < cockpit.SurfaceCount; ++i)
                    {
                        if (config_.sectionExists($"LCD:({block.CustomName}):{i}"))
                            surfaces_.Add(new Surface(this, cockpit.GetSurface(i), i, $"LCD:({block.CustomName}):{i}"));
                    }
                }

                // process modules
                foreach (var module in modules_)
                {
                    if (module.onScanBlock(block))
                        return false;
                }

                return false;
            });

            // scan all groups
            GridTerminalSystem.GetBlockGroups(null, (group) =>
            {
                foreach (var module in modules_)
                {
                    if (module.onScanGroups(group))
                        return false;
                }

                return false;
            });

            // validate all modules
            foreach (var module in modules_)
            {
                module.onValidate();
            }

            stateHandler_ = handleRunningState;
            nextModulePosition_ = 0;
        }


        private void handleRunningState(UpdateType updateType)
        {
            if (!useFastMode_)
            {
                Module module = modules_[nextModulePosition_++];
                module.onProcss(updateType);

                if (nextModulePosition_ >= modules_.Count)
                    nextModulePosition_ = 0;
            }
            else
            {
                foreach (var module in modules_)
                {
                    module.onProcss(updateType);
                }
            }

            UpdateLCDPanels();
        }


        private void handlePanicState(UpdateType updateType)
        {
            Echo("A kernel panic is occurred!");

            // ToDo: Shutoff all panels
            ShutdownLCDPanels();

            // stop automatic updates
            Runtime.UpdateFrequency = UpdateFrequency.None;
        }
        #endregion // System State Handler
        #endregion // RCS System State


        #region Modul System
        /*!
         * Module Base class
         * Use this class for all your modules
         */
        public class Module
        {
            public enum ModuleState
            {
                Invalidate,
                Bootup,
                Running,
                Stopped,
                Resume,
                Failure
            }

            private Program app_ = null;
            private string moduleName_ = "";
            private string moduleId_ = "";
            private ModuleState moduleState_ = ModuleState.Invalidate;


            public Module(Program app, string name, string id)
            {
                app_ = app;
                moduleName_ = name;
                moduleId_ = id;
            }


            #region Properties
            protected Program App
            {
                get
                {
                    return app_;
                }
            }


            public string Name
            {
                get
                {
                    return moduleName_;
                }
            }


            public string Id
            {
                get
                {
                    return moduleId_;
                }
            }


            protected IMyProgrammableBlock Me
            {
                get
                {
                    return app_.Me;
                }
            }


            public ModuleState State
            {
                get
                {
                    return moduleState_;
                }
            }
            #endregion

            #region Event Handler
            /*!
             * If something is going wrong, we need to revalidate all modules. This
             * result in a normal boot sequence. But before we can do it, we need to
             * invalidate all modules. At this point this method is called.
             */
            public virtual void onInvalidate()
            {
                setState(ModuleState.Invalidate);
            }

            public virtual void onValidate()
            {
                setState(ModuleState.Bootup);
            }

            /*!
             * This method is called if the system rescan the grid. If this method
             * returns true the block is handled by this module and cannot be handled
             * by any other module. Otherwise it returns false.
             */
            public virtual bool onScanBlock(IMyTerminalBlock block)
            {
                return false;
            }


            /*!
             * This method is called if the system rescan all groups. If this method
             * returns true the group is handled by this module and cannot be handled
             * by any other module. Otherwise it returns false.
             */
            public virtual bool onScanGroups(IMyBlockGroup group)
            {
                return false;
            }

            /*!
             * 
             */
            public virtual void onProcss(UpdateType updateType)
            {
            }

            /*!
             * If the game want to store something, then this method is called. If you want to save
             * some of your variables, then do it here.
             */
            public virtual void onSaveConfiguration()
            {
            }
            #endregion

            #region Utility Functions
            protected void setState(ModuleState state)
            {
                moduleState_ = state;
            }


            public void Echo(string message)
            {
                App.Echo(message);
            }
            #endregion

            #region Block Tools
            public void activateBlock(IMyTerminalBlock block)
            {
                block.ApplyAction("OnOff_On");
            }

            public void deactivateBlock(IMyTerminalBlock block)
            {
                block.ApplyAction("OnOff_Off");
            }

            public bool isActive(IMyTerminalBlock block)
            {
                return block.GetValue<bool>("OnOff") && block.IsFunctional;
            }

            public bool switchOnOff(IMyTerminalBlock block, bool active)
            {
                bool state = block.GetValue<bool>("OnOff");
                if (state != active)
                {
                    block.SetValue<bool>("OnOff", active);
                    return true;
                }

                return false;
            }

            public bool canBeActivated(IMyTerminalBlock block)
            {
                return !block.GetValue<bool>("OnOff") && block.IsFunctional;
            }
            #endregion
        }


        List<Module> modules_ = null;


        public Module findModuleById(string id)
        {
            foreach (var module in modules_)
            {
                if (module.Id == id)
                    return module;
            }

            return null;
        }


        public Module findMduleByName(string name)
        {
            foreach (var module in modules_)
            {
                if (module.Name == name)
                    return module;
            }

            return null;
        }


        public bool registerModule(Module module)
        {
            Module exist = findModuleById(module.Id);
            if (exist == null)
                modules_.Add(module);

            return false;
        }
        #endregion


        #region Module implementations
        /*!
         * Cockpit Controller
         */
        public class MainRCSSystem : Module
        {
            List<IMyShipController> cockpits_ = null;
            List<IMyGyro> gyros_ = null;
            IMyShipController mainCockpit_ = null;

            bool isParking_ = false;

            Vector3D lastPosition_ = new Vector3D();
            Vector3D directionVector_ = new Vector3D();

            double velocityCurrent_ = 0.0;
            double velocityLastVector_ = 0.0;


            public MainRCSSystem(Program app)
                : base(app, "Main RCS System", "RCS")
            {
                cockpits_ = new List<IMyShipController>();
                gyros_ = new List<IMyGyro>();
            }


            #region Properties
            public List<IMyShipController> Cockpits
            {
                get
                {
                    return cockpits_;
                }
            }


            public IMyShipController MainCockpit
            {
                get
                {
                    if (HasMainCockpit)
                        return mainCockpit_;
                    return null;
                }
            }


            public bool HasMainCockpit
            {
                get
                {
                    return mainCockpit_ != null;
                }
            }


            public bool IsParking
            {
                get
                {
                    return MainCockpit.HandBrake;
                }
            }


            public double TotalGridMass
            {
                get
                {
                    if (cockpits_.Count > 0)
                        return cockpits_[0].CalculateShipMass().TotalMass;

                    return 0.0;
                }
            }


            public Vector3D CurrentPosition
            {
                get
                {
                    return MainCockpit.GetPosition();
                }
            }


            public Vector3D LastPosition
            {
                get
                {
                    return lastPosition_;
                }
            }


            public Vector3D Direction
            {
                get
                {
                    return directionVector_;
                }
            }


            public double Velocity
            {
                get
                {
                    return velocityCurrent_;
                }
            }


            public bool MoveForward
            {
                get
                {
                    return MainCockpit.MoveIndicator.Z < 0 ? true : false;
                }
            }


            public bool MoveBackward
            {
                get
                {
                    return MainCockpit.MoveIndicator.Z > 0 ? true : false;
                }
            }


            public bool MoveLeft
            {
                get
                {
                    return MainCockpit.MoveIndicator.X < 0 ? true : false;
                }
            }


            public bool MoveRight
            {
                get
                {
                    return MainCockpit.MoveIndicator.X > 0 ? true : false;
                }
            }


            public bool Break
            {
                get
                {
                    return MainCockpit.MoveIndicator.Y > 0 || IsParking;
                }
            }


            public float Speed
            {
                get
                {
                    return (float)MainCockpit.GetShipSpeed();
                }
            }
            #endregion

            #region Event Handler
            public override void onInvalidate()
            {
                setState(ModuleState.Invalidate);
                mainCockpit_ = null;
                cockpits_.Clear();
                gyros_.Clear();
            }

            public override bool onScanBlock(IMyTerminalBlock block)
            {
                IMyShipController cockpit = block as IMyShipController;
                if (cockpit != null)
                {
                    cockpits_.Add(cockpit);
                    if (cockpit.IsMainCockpit)
                        mainCockpit_ = cockpit;
                    return false;
                }

                IMyGyro gyro = block as IMyGyro;
                if (gyro != null)
                {
                    gyros_.Add(gyro);
                    return true;
                }

                return false;
            }

            public override void onProcss(UpdateType updateType)
            {
                if (State == ModuleState.Running)
                {
                    /*
                    Echo("RCS:Cockpits=" + cockpits_.Count);
                    Echo("RCS:MainCockpit=" + (HasMainCockpit ? "yes" : "no"));
                    Echo("RCS:IsParking=" + (IsParking ? "yes" : "no"));
                    Echo("RCS:CurrentSpeed=" + MainCockpit.GetShipSpeed().ToString("##0.00"));
                    */

                    // Parking Mode changed
                    if (isParking_ != MainCockpit.HandBrake)
                    {
                        foreach (var gyro in gyros_)
                            switchOnOff(gyro, !MainCockpit.HandBrake);
                        isParking_ = MainCockpit.HandBrake;
                    }

                    // calculate direction vector
                    directionVector_ = MainCockpit.GetPosition() - lastPosition_;
                    lastPosition_ = MainCockpit.GetPosition();

                    // calculate velocity
                    double velocityCurrentVector = MainCockpit.GetShipVelocities().LinearVelocity.LengthSquared();
                    velocityCurrent_ = velocityCurrentVector - velocityLastVector_;
                    velocityLastVector_ = velocityCurrentVector;
                }
                else if (State == ModuleState.Bootup)
                {
                    // if it is not set direktly, use the first cockpit as main cockpit
                    if (mainCockpit_ == null)
                    {
                        if (cockpits_.Count > 0)
                            mainCockpit_ = cockpits_[0];
                        else
                        {
                            setState(ModuleState.Failure); // no cockpit found!
                            return;
                        }
                    }

                    // setup gyros
                    isParking_ = MainCockpit.HandBrake;
                    foreach (var gyro in gyros_)
                        switchOnOff(gyro, !isParking_);

                    lastPosition_ = MainCockpit.GetPosition();
                    directionVector_ = MainCockpit.GetPosition() - lastPosition_;
                    setState(ModuleState.Running);
                }
            }
            #endregion
        }

        /*!
         * Network Controller
         */
        public class NetworkController : Module
        {
            public NetworkController(Program app)
                : base(app, "Network Controller", "NC")
            {
            }

            #region Event Handler
            #endregion
        }


        /*!
         * Docking Controller
         */
        public class DockingController : Module
        {
            List<IMyShipConnector> connectors_ = null;
            List<IMyLightingBlock> lights_ = null;
            MainRCSSystem rcs_ = null;

            // light value
            Color lightColorReady_ = Color.Yellow;
            Color lightColorConnected_ = Color.Green;

            bool isConnected_ = false;
            bool isReadyToConnect_ = false;
            bool isHomeBase_ = false;


            public DockingController(Program app)
                : base(app, "Docking Controller", "DC")
            {
                connectors_ = new List<IMyShipConnector>();
                lights_ = new List<IMyLightingBlock>();
            }

            #region Properties
            public bool IsConnected
            {
                get
                {
                    return isConnected_;
                }
            }


            public bool IsReadyToConnect
            {
                get
                {
                    return isReadyToConnect_ && !isConnected_;
                }
            }


            public bool IsHomeBase
            {
                get
                {
                    return isHomeBase_;
                }
            }
            #endregion

            #region Event Handler
            public override bool onScanBlock(IMyTerminalBlock block)
            {
                IMyShipConnector connector = block as IMyShipConnector;
                if (connector != null)
                {
                    connectors_.Add(connector);
                    return true;
                }

                return false;
            }


            public override bool onScanGroups(IMyBlockGroup group)
            {
                if (group.Name == Program.defaultConnectorLightsGroupName_)
                {
                    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                    group.GetBlocksOfType<IMyLightingBlock>(blocks, (light) =>
                    {
                        lights_.Add(light as IMyLightingBlock);
                        return false;
                    });

                    return true;
                }

                return false;
            }


            public override void onInvalidate()
            {
                setState(ModuleState.Invalidate);
                connectors_.Clear();
                lights_.Clear();
            }


            public override void onValidate()
            {
                // read configuration
                App.Config.getValue($"Module:{Name}", "ColorReady", out lightColorReady_, Color.Yellow);
                App.Config.getValue($"Module:{Name}", "ColorConnected", out lightColorConnected_, Color.Green);

                base.onValidate();
            }


            public override void onProcss(UpdateType updateType)
            {
                if (State == ModuleState.Running)
                {
                    if (rcs_.State != ModuleState.Running)
                        return;

                    bool readyToConnect = false;
                    bool connected = false;
                    bool homeBase = false;

                    // automaticaly connect/disconnect
                    foreach (var connector in connectors_)
                    {
                        if (connector.Status == MyShipConnectorStatus.Connectable)
                        {
                            if (rcs_.IsParking)
                                connector.Connect();

                            readyToConnect = true;
                        }
                        else if (connector.Status == MyShipConnectorStatus.Connected)
                        {
                            if (!rcs_.IsParking)
                                connector.Disconnect();

                            connected = true;
                            readyToConnect = false;

                            // ToDo: find a better way to determine this
                            if (connector.OtherConnector.CustomData.Contains("RCS.Home=true"))
                                homeBase = true;
                        }
                    }

                    // controll lights
                    if (isReadyToConnect_ != readyToConnect)
                    {
                        foreach (var light in lights_)
                        {
                            switchOnOff(light, readyToConnect);
                            light.Color = lightColorReady_;
                            light.Falloff = 2f;
                            light.Intensity = 1f;
                            light.Radius = 5f;
                            light.BlinkIntervalSeconds = 2f;
                            light.BlinkLength = 30f;
                        }
                    }
                    else if (isConnected_ != connected)
                    {
                        foreach (var light in lights_)
                        {
                            switchOnOff(light, readyToConnect);
                            light.Color = lightColorConnected_;
                            light.Falloff = 1f;
                            light.Intensity = 1f;
                            light.Radius = 3f;
                            light.BlinkIntervalSeconds = 0f;
                        }
                    }

                    // store values
                    isReadyToConnect_ = readyToConnect;
                    isConnected_ = connected;
                    isHomeBase_ = homeBase;
                }
                else if (State == ModuleState.Bootup)
                {
                    // wait for Main RCS System
                    rcs_ = App.findModuleById("RCS") as MainRCSSystem;
                    if (rcs_ != null && rcs_.State == ModuleState.Running)
                        setState(ModuleState.Running);
                }
            }


            public override void onSaveConfiguration()
            {
                App.Config.setValue($"Module:{Name}", "ColorReady", lightColorReady_);
                App.Config.setValue($"Module:{Name}", "ColorConnected", lightColorConnected_);
            }
            #endregion
        }


        /*!
         * Manageing Wheels
         */
        public class SuspensionsManager : Module
        {
            MainRCSSystem rcs_ = null;
            DockingController dc_ = null;
            int waitBeforeRun_ = 2;

            // light variables
            List<IMyLightingBlock> breakLights_ = null;
            Color breakLightColor_ = new Color(1f, 0f, 0f);
            float breakLightIntensity_ = 5f;
            float breakLightRadius_ = 5f;
            float breakLightFalloff_ = 2f;
            bool isBreaking_ = false;

            double vehicleLastMass_ = -1000d; // dummy value to force recalculation
            float vehicleMaxSpeed_ = 120f;

            // motor variables
            List<IMyMotorSuspension> motors_ = null;
            double motorHeightReal_ = 0d;
            double motorHeightOffset_ = 0d;
            double motorHeightVariance_ = 0.06d;
            bool motorHeightInvert_ = true;

            const float strengthMin_ = 16f;
            float strengthDestination_ = strengthMin_;
            float strengthIncrease_ = 0.2f;

            float powerDestination_ = 0f;
            float powerMinDestination_ = 20f;
            float powerIncrease_ = 5.0f;


            public SuspensionsManager(Program app)
                : base(app, "Suspensions Manager", "SM")
            {
                motors_ = new List<IMyMotorSuspension>();
                breakLights_ = new List<IMyLightingBlock>();
                vehicleLastMass_ = -1000d;
            }


            private bool getRealHeight(IMyMotorSuspension motor, out double height)
            {
                height = 0;

                if (motor.Top == null)
                    return false;

                var lengthMotor = motor.GetPosition().Length();
                var lengthWheel = motor.Top.GetPosition().Length();
                height = lengthWheel - lengthMotor;

                return true;
            }


            private void calculateStrengthValue()
            {
                // calculate motor strength value
                /*
                Echo("SC:heightDiff=" + heightDifference.ToString("####.#####"));
                Echo("SC:TotalGridMass=" + rcs_.TotalGridMass.ToString("########0.##"));
                Echo("SC:vehicleLastMass_=" + vehicleLastMass_.ToString("########0.##"));
                Echo("SC:motorHeightOffset_=" + motorHeightOffset_.ToString("##0.#####"));
                Echo("SC:motorHeightReal_=" + motorHeightReal_.ToString("##0.#####"));
                */

                if ((Math.Abs(motorHeightOffset_ - motorHeightReal_) > motorHeightVariance_) || (rcs_.TotalGridMass != vehicleLastMass_))
                {
                    bool isLess = motorHeightInvert_ ? ((motorHeightOffset_ + motorHeightVariance_) < motorHeightReal_) : ((motorHeightOffset_ - motorHeightVariance_) > motorHeightReal_);
                    //Echo("SC:isLess=" + isLess);

                    if (rcs_.TotalGridMass > vehicleLastMass_)
                    {
                        if (isLess)
                            strengthDestination_ += strengthIncrease_;
                        else
                            vehicleLastMass_ = rcs_.TotalGridMass;
                    }
                    else //if (rcs_.TotalGridMass < vehicleLastMass_)
                    {
                        if (isLess)
                        {
                            vehicleLastMass_ = rcs_.TotalGridMass;
                            strengthDestination_ += strengthIncrease_;
                        }
                        else
                            strengthDestination_ -= strengthIncrease_;
                    }
                }

                // clamp strength value
                strengthDestination_ = strengthDestination_ < strengthMin_ ? strengthMin_ : (strengthDestination_ > 100f ? 100f : strengthDestination_);
                if (float.IsNaN(strengthDestination_))
                    strengthDestination_ = strengthMin_;
            }


            private void calculatePowerValue()
            {
                // calculate power value
                var shipLV = Vector3D.Normalize(rcs_.MainCockpit.GetShipVelocities().LinearVelocity);
                var fw = Vector3D.Normalize(rcs_.MainCockpit.WorldMatrix.Forward);

                // is this value is > 0 then we are driving forward, if this value is < 0 we driving backward.
                double length = 1 - (shipLV - fw).Length();
                length = double.IsNaN(length) ? 0 : length;

                // we need more power
                if (rcs_.IsParking)
                    powerDestination_ = powerMinDestination_;
                else
                {
                    if ((length < 0 && rcs_.MoveForward) || (length > 0 && rcs_.MoveBackward) ||
                        ((rcs_.MainCockpit.MoveIndicator.Z != 0 && rcs_.Velocity < 0) && (rcs_.Speed - 5 < vehicleMaxSpeed_)))
                        powerDestination_ = 100f;
                    else
                        powerDestination_ = (float)(1 - (rcs_.Speed / ((vehicleMaxSpeed_ * 0.0002777777778f) * 1000f))) * 100f;
                }

                powerDestination_ = Math.Max(powerMinDestination_, float.IsNaN(powerDestination_) ? 0f : powerDestination_);
            }


            #region Event Handler
            public override bool onScanBlock(IMyTerminalBlock block)
            {
                IMyMotorSuspension motor = block as IMyMotorSuspension;
                if (motor != null)
                {
                    motors_.Add(motor);
                    return true;
                }

                return false;
            }


            public override bool onScanGroups(IMyBlockGroup group)
            {
                if (group.Name == Program.defaultBreakLightsGroupName_)
                {
                    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                    group.GetBlocksOfType<IMyLightingBlock>(blocks);
                    foreach (var block in blocks)
                    {
                        breakLights_.Add(block as IMyLightingBlock);
                    }

                    return true;
                }

                return false;
            }


            public override void onInvalidate()
            {
                setState(ModuleState.Invalidate);
                motors_.Clear();
                breakLights_.Clear();
                vehicleLastMass_ = double.MaxValue;
                rcs_ = null;
                dc_ = null;
            }


            public override void onProcss(UpdateType updateType)
            {
                if (State == ModuleState.Running)
                {
                    // we don't need this module keep running
                    if (rcs_.State != ModuleState.Running || dc_.State != ModuleState.Running || dc_.IsConnected)
                        setState(ModuleState.Stopped);

                    double realHeight = 0;
                    double heightOffset = 0;

                    foreach (var motor in motors_)
                    {
                        motor.SetValue<float>("Speed Limit", vehicleMaxSpeed_);

                        double heightValue = 0;
                        if (getRealHeight(motor, out heightValue))
                            realHeight += heightValue;

                        // apply strength value
                        if (rcs_.Speed < 0.1f)
                            motor.Strength = strengthDestination_;
                        heightOffset += motor.Height;


                        // apply power value in soft mode
                        if (Math.Abs(motor.Power - powerDestination_) < powerIncrease_)
                            motor.Power = powerDestination_;
                        else
                        {
                            if (motor.Power < powerDestination_)
                                motor.Power += powerIncrease_;
                            else
                                motor.Power -= powerIncrease_;
                        }
                    }

                    motorHeightReal_ = realHeight / motors_.Count;
                    motorHeightOffset_ = heightOffset / motors_.Count;

                    /*
                    Echo("SC:strengthDestination_=" + strengthDestination_.ToString("##0.#####"));
                    Echo("SC:HeightDifference=" + Math.Abs(motorHeightReal_ - motorHeightOffset_).ToString("##0.#####"));
                    Echo("SC:TotalGridMass=" + rcs_.TotalGridMass.ToString("########0.##"));
                    Echo("SC:vehicleLastMass_=" + vehicleLastMass_.ToString("########0.##"));
                    */

                    if (rcs_.Speed < 0.1f)
                        calculateStrengthValue();

                    calculatePowerValue();

                    // manage break lights
                    bool isBreaking = rcs_.Break || rcs_.MainCockpit.MoveIndicator.Z == 0 && rcs_.Speed > 0.1f;
                    if (isBreaking_ != isBreaking)
                    {
                        isBreaking_ = isBreaking;
                        foreach (var light in breakLights_)
                        {
                            if (isBreaking_)
                            {
                                light.Color = breakLightColor_;
                                light.Falloff = breakLightFalloff_;
                                light.Intensity = breakLightIntensity_;
                                light.Radius = breakLightRadius_;
                                activateBlock(light);
                            }
                            else
                                deactivateBlock(light);
                        }
                    }
                }
                else if (State == ModuleState.Bootup)
                {
                    if (rcs_ == null)
                        rcs_ = App.findModuleById("RCS") as MainRCSSystem;
                    else
                        return;

                    if (dc_ == null)
                        dc_ = App.findModuleById("DC") as DockingController;
                    else
                        return;

                    if (rcs_.State == ModuleState.Running && dc_.State == ModuleState.Running)
                    {
                        // some precalculations
                        float strength = 0f;
                        float power = 0f;
                        foreach (var motor in motors_)
                        {
                            // setup start values
                            strength += motor.Strength;
                            power += motor.Power;
                            activateBlock(motor);
                        }

                        strengthDestination_ = strength / motors_.Count;
                        powerDestination_ = power / motors_.Count;

                        waitBeforeRun_ = 2;
                        setState(ModuleState.Resume);
                    }
                }
                else if (State == ModuleState.Stopped)
                {
                    // we are ready to continue
                    if (rcs_.State == ModuleState.Running && dc_.State == ModuleState.Running &&
                        !dc_.IsConnected)
                    {
                        waitBeforeRun_ = 2;
                        setState(ModuleState.Resume);
                    }
                }
                else if (State == ModuleState.Resume)
                {
                    if (waitBeforeRun_-- <= 0)
                        setState(ModuleState.Running);
                }
            }
            #endregion
        }


        /*!
         * Energie Controller
         */
        public class EnergieManager : Module
        {
            List<IMyBatteryBlock> batteries_ = null;
            List<IMyPowerProducer> generators_ = null;

            DockingController dc_ = null;


            public EnergieManager(Program app)
                : base(app, "Energie Manager", "EM")
            {
                batteries_ = new List<IMyBatteryBlock>();
                generators_ = new List<IMyPowerProducer>();
            }


            #region Properties
            public float BatteryPowerInUsage
            {
                get
                {
                    return batteryPowerInUsage_;
                }
            }


            public float BatteryPowerLeft
            {
                get
                {
                    return batteryPowerLeft_;
                }
            }


            public float GeneratorPowerInUsage
            {
                get
                {
                    return generatorCurrentOutput_ / generatorMaxOutput_;
                }
            }


            public int GeneratorsRunning
            {
                get
                {
                    return generatorRunning_;
                }
            }


            public float EnergieInUsage
            {
                get
                {
                    return (batteryCurrentOutput_ + generatorCurrentOutput_) / (batteryMaxOutput_ + generatorMaxOutput_);
                }
            }
            #endregion

            /*!
             * Battery Management.
             * TODO: Fallback Handling if one ore more batteries are damaged! In this case
             * we going into survival mode and setting all batteris to auto.
             */
            #region Battery Management
            // status variables
            float batteryPowerLeft_ = 0f;
            float batteryPowerInUsage_ = 0f;
            float batteryMaxStored_ = 0f;
            float batteryCurrentStored_ = 0f;
            float batteryMaxOutput_ = 0f;
            float batteryCurrentOutput_ = 0f;

            // Threshold values. All values in percentage. 1 == 100% | 0 == 0%
            float batteryIsFull_ = 0.90f;


            private int rechargeAbleBatteries()
            {
                int active = 0;
                int recharge = 0;

                foreach (var bat in batteries_)
                {
                    if (isActive(bat))
                    {
                        active++;
                        recharge += bat.ChargeMode == ChargeMode.Recharge ? 1 : 0;
                    }
                }

                return active - recharge;
            }


            /*!
             * Manage all batteries that are placed into the grid. If this
             * method has changed one state of a battery the method will
             * return true. Otherwise it will return false.
             */
            private void processBatteries()
            {
                float maxStored = 0f;
                float maxOut = 0f;
                float currentStored = 0f;
                float currentOut = 0f;


                // we are connected, so we want to load all batteries. But we neet to keep at least
                // one battery in discharge or auto mode.
                if (dc_.IsHomeBase)
                {
                    int rechargeAble = rechargeAbleBatteries();

                    foreach (var battery in batteries_)
                    {
                        // collect informations
                        currentOut += battery.CurrentOutput;
                        currentStored += battery.CurrentStoredPower;
                        maxOut += battery.MaxOutput;
                        maxStored += battery.MaxStoredPower;

                        if (battery.ChargeMode != ChargeMode.Recharge)
                        {
                            float curValue = battery.CurrentStoredPower / battery.MaxStoredPower;
                            if (curValue <= batteryIsFull_ && rechargeAble > 1)
                            {
                                battery.ChargeMode = ChargeMode.Recharge;
                                rechargeAble--;
                                continue;
                            }
                        }

                        if (battery.ChargeMode != ChargeMode.Auto)
                            battery.ChargeMode = ChargeMode.Auto;
                    }
                }
                // we are not connected. So we need to manage all batteries
                else
                {
                    foreach (var battery in batteries_)
                    {
                        // battery is deactivated, try to activate
                        if (!isActive(battery))
                        {
                            // if this block can be reactivated, then we wait for the next tick to process the hole state
                            // includ this battery.
                            activateBlock(battery);
                            if (isActive(battery))
                            {
                                battery.ChargeMode = ChargeMode.Auto;
                                return;
                            }
                        }

                        // if powerlevel is low we set all batteries to auto
                        if (batteryPowerLeft_ <= generatorActivateAtPowerLevel_)
                        {
                            if (battery.ChargeMode != ChargeMode.Auto)
                                battery.ChargeMode = ChargeMode.Auto;
                        }
                        // otherwise we have enough power to set all batteries to discharge
                        else
                        {
                            if (battery.ChargeMode != ChargeMode.Discharge)
                                battery.ChargeMode = ChargeMode.Discharge;
                        }

                        // collect informations
                        currentOut += battery.CurrentOutput;
                        currentStored += battery.CurrentStoredPower;
                        maxOut += battery.MaxOutput;
                        maxStored += battery.MaxStoredPower;
                    }
                }

                batteryPowerLeft_ = currentStored / maxStored;
                batteryPowerInUsage_ = currentOut / maxOut;

                batteryMaxOutput_ = maxOut;
                batteryMaxStored_ = maxStored;
                batteryCurrentOutput_ = currentOut;
                batteryCurrentStored_ = currentStored;
                return;
            }
            #endregion

            #region Generator Management
            // Threshold values. All values in percentage. 1 == 100% | 0 == 0%
            float generatorActivateAtPowerLevel_ = 0.5f;
            float generatorActivateAtUsageLevel_ = 0.9f;

            // status values
            bool generatorInConnectedMode_ = false;
            int generatorRunning_ = 0;
            float generatorMaxOutput_ = 0f;
            float generatorCurrentOutput_ = 0f;


            private void calculateGeneratorStats()
            {
                generatorRunning_ = 0;
                generatorInConnectedMode_ = false;
                generatorMaxOutput_ = 0f;
                generatorCurrentOutput_ = 0f;

                foreach (var generator in generators_)
                {
                    generatorRunning_ += isActive(generator) ? 1 : 0;
                    generatorMaxOutput_ += generator.MaxOutput;
                    generatorCurrentOutput_ += generator.CurrentOutput;
                }
            }


            private void processGenerators()
            {
                // shutdown all genertors. We don't need them if we are connected
                if (dc_.IsHomeBase)
                {
                    if (!generatorInConnectedMode_)
                    {
                        foreach (var generator in generators_)
                            deactivateBlock(generator);
                        generatorInConnectedMode_ = true;
                        generatorCurrentOutput_ = 0f;
                        generatorMaxOutput_ = 0f;
                    }

                    return;
                }
                else
                    generatorInConnectedMode_ = false;


                float currentOut = 0f;
                float maxOut = 0f;
                int running = 0;

                foreach (var generator in generators_)
                {
                    currentOut += generator.CurrentOutput;
                    maxOut += generator.MaxOutput;

                    if (isActive(generator))
                    {
                        running++;

                        // we don't need no more power again. So we can deactivate a generator
                        // to save resources
                        if (batteryPowerLeft_ > (1f - generatorActivateAtUsageLevel_))
                        {
                            if (generator.CurrentOutput < (batteryMaxOutput_ - batteryCurrentOutput_))
                            {
                                deactivateBlock(generator);
                                generatorRunning_--;
                                return;
                            }
                        }
                    }
                    else
                    {
                        // check if we need some power
                        if (batteryPowerLeft_ < generatorActivateAtPowerLevel_ ||
                            batteryPowerInUsage_ > generatorActivateAtUsageLevel_)
                        {
                            if (canBeActivated(generator))
                            {
                                generatorRunning_++;
                                activateBlock(generator);
                                return;
                            }
                        }
                    }
                }


                // nothing to do, collect some states
                generatorCurrentOutput_ = currentOut;
                generatorMaxOutput_ = maxOut;
                generatorRunning_ = running;
                return;
            }

            #endregion

            #region Event Handler
            public override bool onScanBlock(IMyTerminalBlock block)
            {
                IMyBatteryBlock battery = block as IMyBatteryBlock;
                if (battery != null)
                {
                    batteries_.Add(battery);
                    return true;
                }

                IMyPowerProducer generator = block as IMyPowerProducer;
                if (generator != null && (generator.BlockDefinition.SubtypeId.Contains("HydrogenEngine") ||
                    generator.BlockDefinition.SubtypeId.Contains("Generator")))
                {
                    generators_.Add(generator);
                    return true;
                }

                return false;
            }


            public override void onInvalidate()
            {
                setState(ModuleState.Invalidate);
                batteries_.Clear();
                generators_.Clear();
                dc_ = null;
            }


            public override void onProcss(UpdateType updateType)
            {
                if (State == ModuleState.Running)
                {
                    if (dc_.State != ModuleState.Running)
                        return;

                    processBatteries();
                    processGenerators();

                    //Echo("EM:GeneratorsRunning=" + generatorRunning_);
                    //Echo("EM:BatteryPowerLeft=" + batteryPowerLeft_.ToString("##0.000"));
                    //Echo("EM:BatteryPowerInUsage=" + batteryPowerInUsage_.ToString("##0.000"));
                    //Echo("EM:BatteryCurrentStored=" + batteryCurrentStored_.ToString("##0.000"));
                    //Echo("EM:BatteryCurrentOutput=" + batteryCurrentOutput_.ToString("##0.000"));
                    //Echo("EM:BatteryMaxOutput=" + batteryMaxOutput_.ToString("##0.000"));
                    //Echo("EM:BatteryMaxStored=" + batteryMaxStored_.ToString("##0.000"));
                    //Echo("EM:HomeBase=" + (dc_.IsHomeBase ? "true" : "false"));
                }
                else if (State == ModuleState.Bootup)
                {
                    if (dc_ == null)
                        dc_ = App.findModuleById("DC") as DockingController;

                    if (dc_ != null && dc_.State == ModuleState.Running)
                    {
                        calculateGeneratorStats();
                        setState(ModuleState.Running);
                    }
                }
            }


            public override void onSaveConfiguration()
            {


                base.onSaveConfiguration();
            }
            #endregion
        }


        /*!
         * Fuel and Inventory Manager
         */
        public class InventoryManager : Module
        {
            int tickCounter_ = 0;

            List<IMyTerminalBlock> inventoryBlocks_ = null;
            double inventoryMaxVolume_ = 0;
            double inventoryFillRatio_ = 0;
            double inventoryIceAmount_ = 0;

            List<IMyGasTank> hydrogenTanks_ = null;
            double hydrogenCapacity_ = 0f;
            double hydrogenFillRatio_ = 0f;

            List<IMyGasTank> oxygenTanks_ = null;
            double oxygenCapacity_ = 0f;
            double oxygenFillRatio_ = 0f;



            public InventoryManager(Program app)
                : base(app, "Inventory Manager", "IM")
            {
                inventoryBlocks_ = new List<IMyTerminalBlock>();
                oxygenTanks_ = new List<IMyGasTank>();
                hydrogenTanks_ = new List<IMyGasTank>();
            }


            #region Properties
            public double InventoryMaxVolume
            {
                get
                {
                    return inventoryMaxVolume_;
                }
            }

            public double InventoryFillRatio
            {
                get
                {
                    return inventoryFillRatio_;
                }
            }


            public double HydrogenCapacity
            {
                get
                {
                    return hydrogenCapacity_;
                }
            }


            public double HydrogenFillRatio
            {
                get
                {
                    return hydrogenFillRatio_;
                }
            }


            public double OxygenCapacity
            {
                get
                {
                    return oxygenCapacity_;
                }
            }


            public double OxygenFillRatio
            {
                get
                {
                    return oxygenFillRatio_;
                }
            }


            public double IceAmount
            {
                get
                {
                    return inventoryIceAmount_;
                }
            }
            #endregion


            #region Event Handler
            public override void onInvalidate()
            {
                inventoryBlocks_.Clear();
                oxygenTanks_.Clear();
                hydrogenTanks_.Clear();
            }


            public override void onValidate()
            {
                tickCounter_ = 0;
                base.onValidate();
            }


            public override bool onScanBlock(IMyTerminalBlock block)
            {
                IMyGasTank tank = block as IMyGasTank;
                if (tank != null)
                {
                    inventoryBlocks_.Add(block);
                    if (tank.DetailedInfo.Contains("Hydrogen"))
                        hydrogenTanks_.Add(tank);
                    else if (tank.DetailedInfo.Contains("Oxygen"))
                        oxygenTanks_.Add(tank);

                    return true;
                }

                if (block.HasInventory && block.InventoryCount > 0)
                    inventoryBlocks_.Add(block);

                return false;
            }


            public override void onProcss(UpdateType updateType)
            {
                if (State == ModuleState.Running)
                {
                    /*
                    Echo("IM:inventoryIceAmount_=" + inventoryIceAmount_);
                    Echo("IM:hydrogenFillRatio_=" + hydrogenFillRatio_);
                    Echo("IM:oxygenFillRatio_=" + oxygenFillRatio_);
                    Echo("IM:inventoryFillRatio=" + inventoryFillRatio_);
                    Echo("IM:tickCounter_=" + tickCounter_);
                    Echo("IM:HydrogenTanks=" + hydrogenTanks_.Count);
                    Echo("IM:OxygenTanks=" + oxygenTanks_.Count);
                    */
                    tickCounter_++;

                    // check hydrogen
                    if (tickCounter_ == 1)
                    {
                        double tankCapacity = 0f;
                        double tankFillRation = 0;

                        foreach (var hydrogen in hydrogenTanks_)
                        {
                            tankCapacity += hydrogen.Capacity;
                            tankFillRation += hydrogen.FilledRatio;
                        }

                        hydrogenCapacity_ = tankCapacity;
                        hydrogenFillRatio_ = tankFillRation / hydrogenTanks_.Count;
                        return;
                    }

                    // check oxygen
                    if (tickCounter_ == 2)
                    {
                        double tankCapacity = 0f;
                        double tankFillRation = 0;

                        foreach (var oxygen in oxygenTanks_)
                        {
                            tankCapacity += oxygen.Capacity;
                            tankFillRation += oxygen.FilledRatio;
                        }

                        oxygenCapacity_ = tankCapacity;
                        oxygenFillRatio_ = tankFillRation / oxygenTanks_.Count;
                        return;
                    }

                    // check inventory
                    if (tickCounter_ == 3)
                    {
                        double maxSize = 0;
                        double used = 0;
                        double ice = 0;

                        foreach (var block in inventoryBlocks_)
                        {
                            for (int c = 0; c < block.InventoryCount; ++c)
                            {
                                var inventory = block.GetInventory(c);
                                maxSize += (double)inventory.MaxVolume;
                                used += (double)inventory.CurrentVolume;
                                ice += (double)inventory.GetItemAmount(MyItemType.MakeOre("Ice"));
                            }
                        }

                        inventoryMaxVolume_ = maxSize;
                        inventoryFillRatio_ = used / maxSize;
                        inventoryIceAmount_ = ice;

                        // restart counter
                        tickCounter_ = 0;
                        return;
                    }
                }
                else if (State == ModuleState.Bootup)
                {
                    setState(ModuleState.Running);
                }
            }
            #endregion
        }
        #endregion // Module implementations


        #region Implement SE Method
        private int failureCounter_ = 0;
        private Exception lastException_ = null;
        private DateTime lastExceptionTime_ = DateTime.Now;


        public Program()
        {
            Runtime.UpdateFrequency = defaultUpdateFrequency_;

            // configuration
            config_ = new Configuration(this);

            // setup defaults
            moduleStateColor_ = new Dictionary<Module.ModuleState, Color>();
            moduleStateColor_.Add(Module.ModuleState.Bootup, Surface.defaultStateColorBootup_);
            moduleStateColor_.Add(Module.ModuleState.Running, Surface.defaultStateColorRunning_);
            moduleStateColor_.Add(Module.ModuleState.Invalidate, Surface.defaultStateColorInvalid_);
            moduleStateColor_.Add(Module.ModuleState.Resume, Surface.defaultStateColorResume_);
            moduleStateColor_.Add(Module.ModuleState.Stopped, Surface.defaultStateColorStopped_);
            moduleStateColor_.Add(Module.ModuleState.Failure, Surface.defaultStateColorFailure_);

            // ToDo: setup your modules here
            modules_ = new List<Module>();
            modules_.Add(new MainRCSSystem(this));
            modules_.Add(new EnergieManager(this));
            modules_.Add(new DockingController(this));
            modules_.Add(new SuspensionsManager(this));
            modules_.Add(new InventoryManager(this));

            // set the hole system as invalid
            stateHandler_ = handleInvalidateState;
        }


        public void Save()
        {
            saveConfiguration();
        }


        public void Main(string argument, UpdateType updateSource)
        {
            // some outputs
            string message = string.Empty;

            message += "Rover Control System (RCS)";
            message += "\n=============================";
            message += "\nVersion:      " + rcsVersion_;
            message += "\nFast Mode: " + (useFastMode_ ? "Enabled" : "Disabled");
            message += "\nLCD's          " + surfaces_.Count;
            message += "\nRun Time:   " + Runtime.LastRunTimeMs.ToString("0.0000" + "ms");
            message += "\nFailures:     " + failureCounter_ + "\n";

            Echo(message);

            // process command line
            if ((updateSource & UpdateType.Terminal) != 0 || (updateSource & UpdateType.Trigger) != 0)
                processCommandLine(argument);

            // process rcs system state
            if ((updateSource & defaultUpdateType_) != 0)
            {
                try
                {
                    if (stateHandler_ != null)
                        stateHandler_(updateSource);
                }
                catch (Exception exp)
                {
                    lastException_ = exp;
                    failureCounter_++;

                    // switch to panic state if we have more then 10 exceptions in 10sec
                    if ((DateTime.Now - lastExceptionTime_) <= TimeSpan.FromSeconds(10.0))
                    {
                        if (failureCounter_ >= 10)
                        {
                            stateHandler_ = handlePanicState;
                            return;
                        }
                    }
                    else
                        failureCounter_ = 0;

                    stateHandler_ = handleInvalidateState;
                }
            }
        }
        #endregion
        // script end
    }
}