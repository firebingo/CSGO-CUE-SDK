﻿using System;
using CUE.NET;
using CUE.NET.Devices.Keyboard;
using CUE.NET.Devices.Generic.Enums;
using CUE.NET.Exceptions;
using CSGSI;
using System.Drawing;
using CUE.NET.Devices.Keyboard.Enums;
using CUE.NET.Gradients;
using CUE.NET.Brushes;

namespace CSGO_K70
{
    class KeyLighter
    {
        public CorsairKeyboard keyboard = null;
        public bool useNumpad = true;

        /// <summary>
        /// Initilize the keylighter
        /// </summary>
        public void Start()
        {
            //initilize the keyboard or return an exception if it fails.
            try
            {
                CueSDK.Initialize();
                Console.WriteLine("CUE Started with " + CueSDK.LoadedArchitecture + "-SDK");

                keyboard = CueSDK.KeyboardSDK;
                if (keyboard != null)
                {
                    keyboard.UpdateMode = UpdateMode.Manual;
                    //keyboard.UpdateFrequency = 1f/30f;
                    if (keyboard.DeviceInfo.Model.Contains("K65"))
                        useNumpad = true;
                    KeyGroups.setupKeyGroups(keyboard, useNumpad);
                }

            }
            catch (CUEException ex)
            {
                Console.WriteLine("CUE Exception!");
                Console.WriteLine("ErrorCode: " + Enum.GetName(typeof(CorsairError), ex.Error));
                keyboard = null;
            }
            catch (WrapperException ex)
            {
                Console.WriteLine("CUE Wrapper Exception!");
                Console.WriteLine("Message:" + ex.Message);
                keyboard = null;
            }
        }

        public void Update(float dt)
        {
            if (Core.currentBomb > 0)
                Core.currentBomb -= dt;

            //This section is discussed in detail in bombBackLight.
            if (Core.deadTime > 0)
                Core.deadTime -= dt;

            if (Core.flashTime > 0 && Core.deadTime <= 0)
                Core.flashTime -= dt;

            if (Core.flashTime <= 0 && Core.deadTime <= 0 && Core.currentBomb > 5)
                flashTimeIncrement();

            //the bomb backlighting is done "per frame" while the bomb is planted instead of just on a new game state.
            if (Core.bombPlanted)
            {
                bombBackLight();
            }

            weaponLights();
            healthLights();
            WASDLights();
        }

        /// <summary>
        /// Handle a new game state.
        /// </summary>
        /// <param name="gs"></param>
        public void handleGameState()
        {
            //team backlight only needs to be done on a new gamestate while the bomb is not planted.
            if (!Core.bombPlanted)
            {
                teamBacklight();
            }
        }

        /// <summary>
        /// Backlight the keyboard with the team colors.
        /// </summary>
        /// <param name="gs"></param>
        void teamBacklight()
        {
            if (Core.isT) //terrorist
            {
                foreach (var key in keyboard.Keys)
                {
                    key.Led.Color = Color.FromArgb(255, 231, 60);
                }
            }
            else //counter-terrorist
            {
                foreach (var key in keyboard.Keys)
                {
                    key.Led.Color = Color.FromArgb(255, 200, 111);
                }
            }
        }

        void WASDLights()
        {
            foreach(var key in KeyGroups.WASD.Keys)
            {
                key.Led.Color = Color.White;
            }
            keyboard[CorsairKeyboardKeyId.Space].Led.Color = Color.White;
            keyboard[CorsairKeyboardKeyId.LeftCtrl].Led.Color = Color.White;
            keyboard[CorsairKeyboardKeyId.LeftShift].Led.Color = Color.White;
        }

        /// <summary>
        /// Backlight for the bomb timer.
        /// </summary>
        void bombBackLight()
        {
            if (Core.currentBomb > 0)
            {
                //full backlight
                //I'm going to rubber duck this so when I look at it in the future i'm not confused.
                //Dead time is used for timing how long the keyboard should NOT be lit.
                //Flash time is used for timing how long the keyboard SHOULD be lit.
                //Dead time ticks down from the start until it hits < 0. Then flash time starts ticking down.
                //Once they both hit < 0 flashTimeIncrement is called which resets flash time, and increments bombTick,
                // which then resets dead time to the next time from bombTimes, or 0.35 if its over the length of bombTimes.
                //While dead time is > 0 the keyboard stays unlit.
                //While dead time is < 0 and flashtime is > 0 the keyboard is lit.
                //None of this matters once currentBomb has reached under 5 seconds and the lights just stay solid.
                if (Core.deadTime > 0 && Core.currentBomb > 5)
                {
                    foreach (var key in keyboard.Keys)
                    {
                        key.Led.Color = Color.Black;
                    }
                }
                else
                {
                    foreach (var key in keyboard.Keys)
                    {
                        key.Led.Color = Color.FromArgb(255, 0, 0);
                    }
                }
                //numpad time ticker
                if (useNumpad)
                {
                    //blank the numpad
                    foreach (var key in KeyGroups.numpad.Keys)
                    {
                        key.Led.Color = Color.Black;
                    }
                    //convert bomb time to a int
                    int intBombTime = (int)Math.Floor(Core.currentBomb);
                    //if the bombtime is 2 digits display the first digit, ex 32 displays 3
                    if (intBombTime.ToString().Length > 1)
                    {
                        int digit = 0;
                        int.TryParse(intBombTime.ToString().Substring(0, 1), out digit);
                        foreach (var key in KeyGroups.numpadDigital[digit].Keys)
                        {
                            key.Led.Color = Color.FromArgb(255, 0, 0);
                        }
                        //turn the two digits into a char array so they can be turned back into single digits
                        char[] cDigit = intBombTime.ToString().ToCharArray();
                        keyboard[findKeypadKey(int.Parse(cDigit[0].ToString()))].Led.Color = Color.FromArgb(0, 255, 0);
                        keyboard[findKeypadKey(int.Parse(cDigit[1].ToString()))].Led.Color = Color.FromArgb(0, 255, 0);
                    }
                    //after the bombtime drops below 10 display each digit
                    else
                    {
                        foreach (var key in KeyGroups.numpadDigital[intBombTime].Keys)
                        {
                            key.Led.Color = Color.FromArgb(255, 0, 0);
                        }
                        keyboard[findKeypadKey(intBombTime)].Led.Color = Color.FromArgb(0, 255, 0);
                    }
                }
            }
            else
            {
                foreach (var key in keyboard.Keys)
                {
                    key.Led.Color = Color.FromArgb(255, 0, 0);
                }
            }
        }

        /// <summary>
        /// handles the increasing flash time for the bomb.
        /// </summary>
        void flashTimeIncrement()
        {
            if (Core.flashTime <= 0)
            {
                //increment the bomb timer then set the next flash time.
                ++Core.bombTick;
                if (Core.bombTick > Core.bombTimes.Count - 1)
                    Core.deadTime = 0.35f;
                else
                    Core.deadTime = Core.bombTimes[Core.bombTick];
                Core.flashTime = Core.flashMax;
            }
        }

        /// <summary>
        /// Handles the lighting of number keys for weapon ammo and etc.
        /// </summary>
        void weaponLights()
        {
            //PRIMARY
            //if the weapon has no reserve just blank the key.
            if (Core.primaryReserve == 0)
                keyboard[CorsairKeyboardKeyId.D1].Led.Color = Color.Black;
            else
            {
                //if the weapon has no ammo loaded turn the key red.
                if (Core.primaryCurrent == 0)
                    keyboard[CorsairKeyboardKeyId.D1].Led.Color = Color.FromArgb(255, 0, 0);
                else
                {
                    //this system makes the key turn from green to red based on how much ammo is left.
                    float r = 0;
                    float g = 0;
                    if (Core.primaryCurrent / Core.primaryMax > 0.5)
                    {
                        g = 1f * 255f;
                        r = (2.0f - ((Core.primaryCurrent / Core.primaryMax) / 0.5f)) * 255f;
                    }
                    else
                    {
                        g = (Core.primaryCurrent / Core.primaryMax) / 0.5f;
                        r = 1f * 255f;
                    }
                    keyboard[CorsairKeyboardKeyId.D1].Led.Color = Color.FromArgb((int)r, (int)g, 0);
                }
            }
            //SECONDARY
            //if the weapon has no reserve just blank the key.
            if (Core.secondaryReserve == 0)
                keyboard[CorsairKeyboardKeyId.D2].Led.Color = Color.Black;
            else
            {
                //if the weapon has no ammo loaded turn the key red.
                if (Core.secondaryCurrent == 0)
                    keyboard[CorsairKeyboardKeyId.D2].Led.Color = Color.FromArgb(255, 0, 0);
                else
                {
                    //this system makes the key turn from green to red based on how much ammo is left.
                    float r = 0;
                    float g = 0;
                    if (Core.secondaryCurrent / Core.secondaryMax > 0.5)
                    {
                        g = 1f * 255f;
                        r = (2.0f - ((Core.secondaryCurrent / Core.secondaryMax) / 0.5f)) * 255f;
                    }
                    else
                    {
                        g = (Core.secondaryCurrent / Core.secondaryMax) / 0.5f;
                        r = 1f * 255f;
                    }
                    keyboard[CorsairKeyboardKeyId.D2].Led.Color = Color.FromArgb((int)r, (int)g, 0);
                }
            }

            //You always have a knife so keep the key green.
            keyboard[CorsairKeyboardKeyId.D3].Led.Color = Color.FromArgb(0, 255, 0);

            //if you have grenades turn the key green otherwise blank it.
            if (Core.grenadeCount == 0)
                keyboard[CorsairKeyboardKeyId.D4].Led.Color = Color.Black;
            else
                keyboard[CorsairKeyboardKeyId.D4].Led.Color = Color.FromArgb(0, 255, 0);

            //if you have C4 turn the key green otherwise blank it.
            if (Core.hasC4)
                keyboard[CorsairKeyboardKeyId.D5].Led.Color = Color.Black;
            else
                keyboard[CorsairKeyboardKeyId.D5].Led.Color = Color.FromArgb(0, 255, 0);
        }

        /// <summary>
        /// Handles the lighting of the function keys for health and armor values
        /// </summary>
        void healthLights()
        {
            //HEALTH
            if (Core.healthCurrent == 0) //if no health blank keys
            {
                foreach (var key in KeyGroups.healthFunction.Keys)
                {
                    key.Led.Color = Color.Black;
                }
            }
            else if (Core.healthCurrent == Core.healthMax) //if health is full set keys to green
            {
                foreach (var key in KeyGroups.healthFunction.Keys)
                {
                    key.Led.Color = Color.FromArgb(0, 255, 0);
                }
            }
            else //uses a gradient brush to have the health key group act as a bar.
            {
                GradientStop[] healthGrad =
                {
                    new GradientStop(Core.healthCurrent / Core.healthMax, Color.FromArgb(0, 255, 0)),
                    new GradientStop(1f, Color.Black)
                };
                LinearGradient healthGradient = new LinearGradient(healthGrad);
                PointF startPoint = new PointF(0f, 0.5f);
                PointF endPoint = new PointF(1f, 0.5f);
                LinearGradientBrush healthBrush = new LinearGradientBrush(startPoint, endPoint, healthGradient);
                KeyGroups.healthFunction.Brush = healthBrush;
            }

            //ARMOR
            Color armorColor = Color.Black;
            if (Core.hasHelmet)
                armorColor = Color.FromArgb(0, 0, 255);
            else
                armorColor = Color.FromArgb(0, 128, 255);
            if (Core.armorCurrent == 0) //if no armor turn keys red
            {
                foreach (var key in KeyGroups.armorFunction.Keys)
                {
                    key.Led.Color = Color.FromArgb(255, 0, 0);
                }
            }
            else if (Core.armorCurrent == Core.armorMax) //if armor is full set keys to armor color
            {
                foreach (var key in KeyGroups.armorFunction.Keys)
                {
                    key.Led.Color = armorColor;
                }
            }
            else //uses a gradient brush to have the armor key group act as a bar.
            {
                GradientStop[] armorGrad =
                {
                    new GradientStop(Core.armorCurrent / Core.armorMax, armorColor),
                    new GradientStop(1f, Color.Black)
                };
                LinearGradient armorGradient = new LinearGradient(armorGrad);
                PointF startPoint = new PointF(1f, 0.5f);
                PointF endPoint = new PointF(0f, 0.5f);
                LinearGradientBrush armorBrush = new LinearGradientBrush(startPoint, endPoint, armorGradient);
                KeyGroups.armorFunction.Brush = armorBrush;
            }
        }

        /// <summary>
        /// Finds a keypad number based on the number fed in.
        /// Only for single digits.
        /// </summary>
        CorsairKeyboardKeyId findKeypadKey(int inI)
        {
            switch (inI)
            {
                case 1:
                    return CorsairKeyboardKeyId.Keypad1;
                case 2:
                    return CorsairKeyboardKeyId.Keypad2;
                case 3:
                    return CorsairKeyboardKeyId.Keypad3;
                case 4:
                    return CorsairKeyboardKeyId.Keypad4;
                case 5:
                    return CorsairKeyboardKeyId.Keypad5;
                case 6:
                    return CorsairKeyboardKeyId.Keypad6;
                case 7:
                    return CorsairKeyboardKeyId.Keypad7;
                case 8:
                    return CorsairKeyboardKeyId.Keypad8;
                case 9:
                    return CorsairKeyboardKeyId.Keypad9;
            }
            return CorsairKeyboardKeyId.Keypad0;
        }
    }
}