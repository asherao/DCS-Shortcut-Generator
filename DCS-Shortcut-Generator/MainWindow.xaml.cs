﻿using System;
using System.Windows;
using System.Windows.Input;
using IWshRuntimeLibrary;
using LsonLib;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;

/* Possible future upgrades:
 * - Expand the image selection to .bmp
 *     - Expand the image selection to any selected image
 * - Integrate OptionsPresets swaps
 * - Have a "Pull Lua" feature so the user does not have to manually copy/paste into their folder
 * - Adjust Backup creation logic. Currently the backup is of the file that it replaced
 * - Aut odetect the users dcs path
 */

namespace DCS_Shortcut_Generator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Process dcsProcess;
        private static bool shouldUpdateOptions;
        private static string currentOptionFile;
        private static string newOptionFileName;

        public MainWindow()
        {
            //https://wpf-tutorial.com/wpf-application/command-line-parameters/
            //https://stackoverflow.com/questions/9343381/wpf-command-line-arguments-a-smart-way
            string[] args = Environment.GetCommandLineArgs(); //put the stuff in Target in an array
            //System.Windows.MessageBox.Show(args[1]);//show the first argument. debug
            if (args.Length > 1) //if there is more than 1 thing in Target, then process it as a shortcut
                //instead of running the generator
            {
                processAsShortcut(); //go to what used to be the console application
                if (dcsProcess != null && shouldUpdateOptions)
                {
                    Application.Current.MainWindow.WindowState = WindowState.Minimized;
                    dcsProcess.EnableRaisingEvents = true;
                    dcsProcess.Exited += (sender, e) =>
                    {
                        System.IO.File.Delete(newOptionFileName + ".prev");
                        System.IO.File.Move(newOptionFileName,
                            newOptionFileName + ".prev");
                        System.IO.File.Copy(currentOptionFile, newOptionFileName);
                        Application.Current.Shutdown();
                    };
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }

            //else...run the generator
            InitializeComponent();
            PredictUserDcsExePath();
            PredictUserOptionsLuaPath();
        }

        private void PredictUserOptionsLuaPath() //prediction for the users default options lua file
        {
            string userName = System.Environment.UserName;

            string userOptionsStableLocation = @"C:\Users\" + userName + @"\Saved Games\DCS\Config\options.lua";
            string userOptionsBetaLocation = @"C:\Users\" + userName + @"\Saved Games\DCS.openbeta\options.lua";
            string userOptionsAlphaLocation = @"C:\Users\" + userName + @"\Saved Games\DCS.openalpha\options.lua";

            //these are to assist the user in locating their correct directory by auto-populating it on load
            if (System.IO.File.Exists(userOptionsStableLocation))
            {
                textBlock_userOptionsFile.Text = userOptionsStableLocation;
                //MessageBox.Show("Case 1");//debug
            }
            else if (System.IO.File.Exists(userOptionsBetaLocation))
            {
                textBlock_userOptionsFile.Text = userOptionsBetaLocation;
                //MessageBox.Show("Case 2");//debug
            }
            else if (System.IO.File.Exists(userOptionsAlphaLocation))
            {
                textBlock_userOptionsFile.Text = userOptionsAlphaLocation;
                //MessageBox.Show("Case 3");//debug
            }
            else
            {
                //do nothing and leave it blank
                //MessageBox.Show("Case 4");//debug
            }
        }

        private void PredictUserDcsExePath()
        {
            //this will attempt to read the registry for the dcs.exe location
            //it will try dcs stable, beta, but not steam at the moment.
            //if it does not find any of the above, the field will revert to nothing, as usual

            /* NOTES
            //https://docs.microsoft.com/en-us/dotnet/api/microsoft.win32.registry.getvalue?view=net-6.0
            //@"HKEY_CURRENT_USER\Software\Eagle Dynamics\DCS World OpenBeta\Path";
            //@"HKEY_CURRENT_USER\Software\Eagle Dynamics\DCS World\Path";
            string[] strings = { "0" };
            const string keyNameBeta = @"HKEY_CURRENT_USER\SOFTWARE\Eagle Dynamics\DCS World OpenBeta";
            const string keyNameStable = @"HKEY_CURRENT_USER\Software\Eagle Dynamics\DCS World";
            const string keyNameSteam = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 223750";//???TODO: find this out
            //is it @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 223750" InstallLocation?

            string dcsLocation = (string)Registry.GetValue(keyNameBeta, "Path", strings);//results in "C:\Games\DCS World OpenBeta"
                                                                                         //add "\bin\DCS.exe"
                                                                                         //string dcsLocation = (string)Registry.GetValue(keyNameStable, "Path", strings);
                                                                                         //string dcsLocation = (string)Registry.GetValue(keyNameSteam, "InstallLocation", strings);
            */

            //this is the DCS Steam Attempt. It is not working. TODO: get this to work
            /*
            if (String.IsNullOrEmpty(textBlock_userDcsExeFile.Text))
            {
                try
                {
                    string[] strings = { "0" };
                    const string keyNameSteam = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 223750";
                    string dcsLocation = (string)Registry.GetValue(keyNameSteam, "InstallLocation", strings);
                    textBlock_userDcsExeFile.Text = dcsLocation;
                    System.Windows.MessageBox.Show(dcsLocation);
                }
                catch
                {
                    //leave blank because there is nothing to do but to continue and use the default
                }
            }
            */

            //this is the open beta attampt
            //if the field is empty, which it should be because the program just booted
            if (String.IsNullOrEmpty(textBlock_userDcsExeFile.Text))
            {
                try
                {
                    string[] strings = { "0" }; //init a string array
                    const string
                        keyNameBeta =
                            @"HKEY_CURRENT_USER\SOFTWARE\Eagle Dynamics\DCS World OpenBeta"; //this is the key we are looking for
                    string dcsLocation =
                        (string)Registry.GetValue(keyNameBeta, "Path", strings); //"Path is the subkey(?)
                    string
                        addOnPath =
                            @"bin\DCS.exe"; //the above terminates in the main folder path. use this to continue to the exe
                    string[] paths = { dcsLocation, addOnPath }; //prepariong for the Combine
                    textBlock_userDcsExeFile.Text = Path.Combine(paths); //populate the text field with the result
                    //System.Windows.MessageBox.Show(dcsLocation);//debug
                    //System.Windows.MessageBox.Show(addOnPath);//debug
                    //System.Windows.MessageBox.Show(textBlock_userDcsExeFile.Text);//debug
                }
                catch
                {
                    //leave blank because there is nothing to do but continue and use the default
                }
            }

            //same as abive, but for dcs stable version
            if (String.IsNullOrEmpty(textBlock_userDcsExeFile.Text))
            {
                try
                {
                    string[] strings = { "0" };
                    const string keyNameStable = @"HKEY_CURRENT_USER\Software\Eagle Dynamics\DCS World";
                    string dcsLocation = (string)Registry.GetValue(keyNameStable, "Path", strings);
                    string addOnPath = @"bin\DCS.exe";
                    string[] paths = { dcsLocation, addOnPath };
                    textBlock_userDcsExeFile.Text = Path.Combine(paths);
                    //System.Windows.MessageBox.Show(dcsLocation);
                    //System.Windows.MessageBox.Show(addOnPath);
                    //System.Windows.MessageBox.Show(textBlock_userDcsExeFile.Text);
                }
                catch
                {
                    //leave blank because there is nothing to do but continue and use the default
                }
            }
        }

        private void processAsShortcut()
        {
            string[] args = Environment.GetCommandLineArgs(); //put the stuff in Target in an array
            //https://stackoverflow.com/questions/27965131/how-to-remove-the-first-element-in-an-array/27965285
            args = args.Skip(1).ToArray(); //this removed the first element of the array, which was the path of this app

            //the minimum args length will be 3 due to the condition checking of the generator

            if (args.Length == 4) //the options lua was defined. do the same thing as Length == 2
            {
                Console.WriteLine("Condition 3");

                string arg_dcsVrOrNoVr = args[1];
                string vrargument;
                string arg_dcsOptionsLuaFileLocation;


                if (System.IO.File.Exists(args[2])) //if the new options lua file does not exist, exit
                {
                    arg_dcsOptionsLuaFileLocation = args[2]; //will remain unused in this Condition
                }
                else
                {
                    Console.WriteLine("The DCS Options.lua file cannot be found.");
                    MessageBox.Show("The DCS Options.lua file cannot be found.");
                    return;
                }

                string arg_dcsExeFileLocation;
                if (System.IO.File.Exists(args[0]))
                {
                    arg_dcsExeFileLocation = args[0];
                    Console.WriteLine(arg_dcsExeFileLocation);
                    Console.WriteLine(arg_dcsVrOrNoVr);

                    if (arg_dcsVrOrNoVr.Equals("vr"))
                    {
                        vrargument = ("--force_enable_VR");
                    }
                    else if (arg_dcsVrOrNoVr.Equals("novr"))
                    {
                        vrargument = ("--force_disable_VR");
                    }
                    else
                    {
                        Console.WriteLine("The VR argument is not valid.");
                        MessageBox.Show("The VR argument is not valid.");
                        return; //the vr argument was not valid
                    }

                    dcsProcess = Process.Start(@arg_dcsExeFileLocation, vrargument);
                }
                else
                {
                    Console.WriteLine("The DCS exe path cannot be found.");
                    MessageBox.Show("The DCS exe path cannot be found.");
                    return; //the path that the user entered did not exist
                }
            } //end of condition 3

            else if (args.Length == 5) //either width was entered or the new dcs options lua was defined
            {
                //if the last arg is a number, then this fails. 
                //If its a path, then use it to replace the options lua path
                //due to how this version of the program works, you should never see just 1 dimension
                Console.WriteLine("Condition 4");

                //file exists checks
                if (!System.IO.File.Exists(args[0]))
                {
                    Console.WriteLine("The DCS exe path cannot be found.");
                    MessageBox.Show("The DCS exe path cannot be found.");
                    return;
                }

                if (!System.IO.File.Exists(args[2]))
                {
                    Console.WriteLine("The DCS Options.lua file cannot be found.");
                    MessageBox.Show("The DCS Options.lua file cannot be found.");
                    return;
                }

                string arg_dcsVrOrNoVr = args[1];
                string vrargument;
                //determine if the user wants to use VR or not
                if (arg_dcsVrOrNoVr.Equals("vr"))
                {
                    vrargument = ("--force_enable_VR");
                }
                else if (arg_dcsVrOrNoVr.Equals("novr"))
                {
                    vrargument = ("--force_disable_VR");
                }
                else //if there isnt a matching entry, just quit
                {
                    Console.WriteLine("The VR argument is not valid.");
                    MessageBox.Show("The VR argument is not valid.");
                    return; //the vr argument was not valid
                }

                string arg_dcsExeFileLocation = args[0];

                string arg_dcsOptionsLuaFileLocation = args[2];
                string arg_widthOrDcsNewOptionsLuaFileLocation = args[3];

                bool updateOptions = bool.Parse(args[4]);

                int arg_width; //init the int for the width of the screen
                bool success = int.TryParse(args[4], out arg_width); //if it can be parsed

                if (success) //if "success" is true, then the parse was good and it's an int
                {
                    Console.WriteLine(arg_width + " is an int."); //debug
                    //because of this we revert to a "condition 2", from the old version
                }
                else //if "success" is false, then the parse was bad and it's a string
                {
                    Console.WriteLine(arg_widthOrDcsNewOptionsLuaFileLocation + " is an string."); //debug


                    SwapOptionFiles(arg_widthOrDcsNewOptionsLuaFileLocation, arg_dcsOptionsLuaFileLocation,
                        updateOptions);
                }

                Console.WriteLine(arg_dcsExeFileLocation); //debug
                Console.WriteLine(arg_dcsVrOrNoVr); //debug
                Console.WriteLine(arg_widthOrDcsNewOptionsLuaFileLocation); //debug

                dcsProcess = Process.Start(@arg_dcsExeFileLocation, vrargument);
            } //end of condition 4

            else if (args.Length == 6) //only width and height was entered. replace the original option
            {
                //lua with them
                Console.WriteLine("Condition 5");

                //file exists checks
                if (!System.IO.File.Exists(args[0]))
                {
                    Console.WriteLine("The DCS exe path cannot be found.");
                    MessageBox.Show("The DCS exe path cannot be found.");
                    return;
                }

                if (!System.IO.File.Exists(args[2]))
                {
                    Console.WriteLine("The DCS Options.lua file cannot be found.");
                    MessageBox.Show("The DCS Options.lua file cannot be found.");
                    return;
                }

                string arg_dcsVrOrNoVr = args[1];
                string vrargument;
                //determine if the user wants to use VR or not
                if (arg_dcsVrOrNoVr.Equals("vr"))
                {
                    vrargument = ("--force_enable_VR");
                }
                else if (arg_dcsVrOrNoVr.Equals("novr"))
                {
                    vrargument = ("--force_disable_VR");
                }
                else //if there isnt a matching entry, just quit
                {
                    Console.WriteLine("The VR argument is not valid.");
                    MessageBox.Show("The VR argument is not valid.");
                    return; //the vr argument was not valid
                }

                string arg_dcsExeFileLocation = args[0];

                string arg_dcsOptionsLuaFileLocation = args[2];

                int arg_width;
                bool success_width = int.TryParse(args[4], out arg_width);


                int arg_height; //init the int for the height of the screen
                bool success_height = int.TryParse(args[5], out arg_height); //if it can be parsed

                if (success_width) //if "success" is true, then the parse was good and it's an int
                {
                    if (success_height) //if "success" is true again, then the parse was good and it's an int
                    {
                        Console.WriteLine(arg_width + " is the width."); //debug
                        Console.WriteLine(arg_height + " is the height."); //debug
                    }
                    else //if "success" is false, then the parses were bad
                    {
                        Console.WriteLine("The the height argument is not valid.");
                        MessageBox.Show("The the height argument is not valid.");
                        return; //quit
                    }
                }
                else //if "success" is false, then the parses were bad
                {
                    Console.WriteLine("The the width argument is not valid.");
                    MessageBox.Show("The the width argument is not valid.");
                    return; //quit
                }

                Console.WriteLine(arg_dcsExeFileLocation); //debug
                Console.WriteLine(arg_dcsVrOrNoVr); //debug

                var optionsLuaContents =
                    LsonVars.Parse(
                        System.IO.File.ReadAllText(
                            arg_dcsOptionsLuaFileLocation)); //put the contents of the options lua file into a lua read
                optionsLuaContents["options"]["graphics"]["width"] = arg_width; //swap in the users width
                optionsLuaContents["options"]["graphics"]["height"] = arg_height; //swap in the users height
                System.IO.File.WriteAllText(arg_dcsOptionsLuaFileLocation,
                    LsonVars.ToString(optionsLuaContents)); // serialize back to a file

                dcsProcess = Process.Start(@arg_dcsExeFileLocation, vrargument); //run the program
            } //end of condition 5
            else if (args.Length == 7) //every arg has been fulfilled
            {
                //first replace the options lua
                //then replace the width and height
                Console.WriteLine("Condition 6");

                //file exists checks
                if (!System.IO.File.Exists(args[0]))
                {
                    Console.WriteLine("The DCS exe path cannot be found.");
                    MessageBox.Show("The DCS exe path cannot be found.");
                    return;
                }

                if (!System.IO.File.Exists(args[2]))
                {
                    Console.WriteLine("The DCS Options.lua file cannot be found.");
                    MessageBox.Show("The DCS Options.lua file cannot be found.");
                    return;
                }

                if (!System.IO.File.Exists(args[3]))
                {
                    Console.WriteLine("The replacement DCS Options.lua file cannot be found.");
                    MessageBox.Show("The replacement DCS Options.lua file cannot be found.");
                    return;
                }

                string arg_dcsVrOrNoVr = args[1];
                string vrargument;
                //determine if the user wants to use VR or not
                if (arg_dcsVrOrNoVr.Equals("vr"))
                {
                    vrargument = ("--force_enable_VR");
                }
                else if (arg_dcsVrOrNoVr.Equals("novr"))
                {
                    vrargument = ("--force_disable_VR");
                }
                else //if there isnt a matching entry, just quit
                {
                    Console.WriteLine("The VR argument is not valid.");
                    MessageBox.Show("The VR argument is not valid.");
                    return; //the vr argument was not valid
                }


                string arg_dcsExeFileLocation = args[0];

                string arg_dcsOptionsLuaFileLocation = args[2];
                string arg_dcsNewOptionsLuaFileLocation = args[3];
                bool updateOptions = bool.Parse(args[4]);

                int arg_width;
                bool success_width = int.TryParse(args[5], out arg_width);
                int arg_height; //init the int for the height of the screen
                bool success_height = int.TryParse(args[6], out arg_height); //if it can be parsed

                if (success_width) //if "success" is true, then the parse was good and it's an int
                {
                    if (success_height) //if "success" is true again, then the parse was good and it's an int
                    {
                        Console.WriteLine(arg_width + " is the width."); //debug
                        Console.WriteLine(arg_height + " is the height."); //debug
                    }
                    else //if "success" is false, then the parses were bad
                    {
                        Console.WriteLine("The the height argument is not valid.");
                        MessageBox.Show("The the height argument is not valid.");
                        return; //quit
                    }
                }
                else //if "success" is false, then the parses were bad
                {
                    Console.WriteLine("The the width argument is not valid.");
                    MessageBox.Show("The the width argument is not valid.");
                    return; //quit
                }

                Console.WriteLine(arg_dcsExeFileLocation); //debug
                Console.WriteLine(arg_dcsVrOrNoVr); //debug
                Console.WriteLine(arg_dcsOptionsLuaFileLocation); //debug
                Console.WriteLine(arg_dcsNewOptionsLuaFileLocation); //debug

                SwapOptionFiles(arg_dcsNewOptionsLuaFileLocation, arg_dcsOptionsLuaFileLocation, updateOptions);

                Console.WriteLine("Width is: " + arg_width + ". Height is: " + arg_height);
                var optionsLuaContents =
                    LsonVars.Parse(
                        System.IO.File.ReadAllText(
                            arg_dcsOptionsLuaFileLocation)); //put the contents of the options lua file into a lua read
                optionsLuaContents["options"]["graphics"]["width"] = arg_width; //swap in the users width
                optionsLuaContents["options"]["graphics"]["height"] = arg_height; //swap in the users height
                System.IO.File.WriteAllText(arg_dcsOptionsLuaFileLocation,
                    LsonVars.ToString(optionsLuaContents)); // serialize back to a file

                dcsProcess = Process.Start(@arg_dcsExeFileLocation, vrargument); //run the program
            } //end of condition 6
        }

        private static void SwapOptionFiles(string arg_dcsNewOptionsLuaFileLocation,
            string arg_dcsOptionsLuaFileLocation, bool updateOptions)
        {
            shouldUpdateOptions = updateOptions;
            currentOptionFile = arg_dcsOptionsLuaFileLocation;
            newOptionFileName = arg_dcsNewOptionsLuaFileLocation;
            System.IO.File.Delete(arg_dcsNewOptionsLuaFileLocation + ".bak"); //deletes the backup file
            System.IO.File.Move(arg_dcsOptionsLuaFileLocation,
                arg_dcsNewOptionsLuaFileLocation + ".bak"); //moves the original options lua file to the backup location
            //File.Replace(arg_dcsNewOptionsLuaFileLocation, arg_dcsOptionsLuaFileLocation, null);//null or "Options.lua.bak"
            //Console.WriteLine("Replacing " + arg_dcsNewOptionsLuaFileLocation + "with " + arg_dcsOptionsLuaFileLocation);
            System.IO.File.Copy(arg_dcsNewOptionsLuaFileLocation,
                arg_dcsOptionsLuaFileLocation); //puts the replacement file into the original location
        }

        //https://stackoverflow.com/questions/10315188/open-file-dialog-and-select-a-file-using-wpf-controls-and-c-sharp

        //https://stackoverflow.com/questions/4897655/create-a-shortcut-on-desktop
        //private void CreateShortcut()
        //{
        //    object shDesktop = (object)"Desktop";
        //    WshShell shell = new WshShell();
        //    string shortcutAddress = (string)shell.SpecialFolders.Item(ref shDesktop) + @"\Notepad.lnk";//this determines the name of the shortcut
        //    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
        //    shortcut.Description = "New shortcut for a Notepad";//this is the description field in the shortcut
        //    shortcut.Hotkey = "Ctrl+Shift+N";//hotkeys to launch the shortcut
        //    shortcut.Arguments = "\"arg1\" " + "\"arg2\" " + "\"arg3\" ";//these are the arguments of the shortcut. remember to quote them and space seperate them
        //    shortcut.TargetPath = Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\notepad.exe";//this is the exe target path
        //    shortcut.Save();
        //}

        //https://stackoverflow.com/questions/1268552/how-do-i-get-a-textbox-to-only-accept-numeric-input-in-wpf
        //If you want only letters then replace the regular expression as [^a-zA-Z]
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void NumberDigitValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^A-Za-z0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void CreateShortcut()
        {
            string shortcutName = textBlock_userShortcutName.Text;
            string appExeFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string shortcutDescription = textBlock_userShortcutDescription.Text;
            //args
            string dcsExeFile = textBlock_userDcsExeFile.Text;
            string dcsOptionsFile = textBlock_userOptionsFile.Text;


            string dcsVrOption;
            if (comboBox_userVR.SelectedIndex == 0) //0 is disabled, 1 is enabled
            {
                dcsVrOption = "novr";
            }
            else
            {
                dcsVrOption = "vr";
            }

            //https://stackoverflow.com/questions/1177872/strip-double-quotes-from-a-string-in-net

            string dcsOptionsNewFile = textBlock_userOptionsNewFile.Text; //could be blank
            //don't need to convert these because we already limited their input to numbers
            string dcsWidth = textBlock_dcsWidth.Text; //could be blank
            string dcsHeight = textBlock_dcsHeight.Text; //could be blank

            //string character length check of less than 259
            string generatedArguments = "\"" + dcsExeFile + "\" " +
                                        "\"" + dcsVrOption + "\" " +
                                        "\"" + dcsOptionsFile + "\" " +
                                        "\"" + dcsOptionsNewFile + "\" " +
                                        "\"" + checkBox_updateOptions.IsChecked + "\" " +
                                        "\"" + dcsWidth + "\" " +
                                        "\"" + dcsHeight + "\"";

            string generatedTarget = appExeFile + " " + generatedArguments;

            generatedTarget =
                generatedTarget.Replace("\"\"",
                    ""); //replace double quotes with nothing. this will "auto format" the arguments

            //System.Windows.MessageBox.Show(generatedTarget + " is " + generatedTarget.Length + " characters long.");//debug

            if (generatedTarget.Length > 259)
            {
                int numberOfCharactersOver = generatedTarget.Length - 259;
                System.Windows.MessageBox.Show("Export Target is " + numberOfCharactersOver +
                                               " characters too long. Export canceled, sorry. " +
                                               "\nSee README for details. Please define shorter paths.");
                return;
            }

            WshShell shell = new WshShell();
            string shortcutAddress = shortcutName + ".lnk";
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = shortcutDescription;

            //this is doubled up for some reason, above
            generatedArguments = "\"" + dcsExeFile + "\" " +
                                 "\"" + dcsVrOption + "\" " +
                                 "\"" + dcsOptionsFile + "\" " +
                                 "\"" + dcsOptionsNewFile + "\" " +
                                 "\"" + checkBox_updateOptions.IsChecked + "\" " +
                                 "\"" + dcsWidth + "\" " +
                                 "\"" + dcsHeight + "\"";

            generatedArguments =
                generatedArguments.Replace("\"\"",
                    ""); //replace double quotes with nothing. this will "auto format" the arguments

            shortcut.Arguments = generatedArguments;

            shortcut.TargetPath = appExeFile;

            if (!String.IsNullOrEmpty(textBlock_userIconLocation.Text)) //if the user picked an icon, use it
            {
                shortcut.IconLocation = textBlock_userIconLocation.Text;
            }

            shortcut.Save(); //saves and exports the shortcut file
            System.Windows.MessageBox.Show("Export Success! Exported to: " +
                                           System.IO.Path.GetDirectoryName(appExeFile).ToString());
            //Yay!!!
        }

        private void button_userDcsExeFile_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".exe";
            dlg.Filter = "exe files (*.exe)|*.exe"; //pick an exe only

            dlg.Title = "Example: " + @"C:\Program Files\Eagle Dynamics\DCS World\bin\DCS.exe";
            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                textBlock_userDcsExeFile.Text = filename;
            }
        }

        private void button_userOptionsFile_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".lua";
            dlg.Filter = "lua files (*.lua)|*.lua"; //pick a lua only

            string userName = System.Environment.UserName;
            dlg.Title = "Example: " + @"C:\Users\" + userName + @"\Saved Games\DCS\Config\options.lua";

            string userOptionsStableLocation = @"C:\Users\" + userName + @"\Saved Games\DCS\Config";
            string userOptionsBetaLocation = @"C:\Users\" + userName + @"\Saved Games\DCS.openbeta\Config";
            string userOptionsAlphaLocation = @"C:\Users\" + userName + @"\Saved Games\DCS.openalpha\Config";

            if (Directory.Exists(userOptionsStableLocation))
            {
                dlg.InitialDirectory = userOptionsStableLocation;
            }
            else if (Directory.Exists(userOptionsBetaLocation))
            {
                dlg.InitialDirectory = userOptionsBetaLocation;
            }
            else if (Directory.Exists(userOptionsAlphaLocation))
            {
                dlg.InitialDirectory = userOptionsAlphaLocation;
            }
            else
            {
                dlg.InitialDirectory = @"C:\Users\" + userName + @"\Saved Games";
            }

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                textBlock_userOptionsFile.Text = filename;
            }
        }

        private void button_export_Click(object sender, RoutedEventArgs e)
        {
            //if the minimum parameters are met, then
            if (textBlock_userDcsExeFile.Text.Equals("")) //nothing was entered
            {
                System.Windows.MessageBox.Show("Select your DCS exe file.");
                return;
            }

            if (textBlock_userOptionsFile.Text.Equals("")) //nothing was entered
            {
                System.Windows.MessageBox.Show("Select your Options lua file.");
                return;
            }

            if (textBlock_userShortcutName.Text.Equals("")) //nothing was entered
            {
                System.Windows.MessageBox.Show("Create a shortcut name.");
                return;
            }

            //check to make sure the user did not enter only one height or width override

            //https://stackoverflow.com/questions/34298857/check-whether-a-textbox-is-empty-or-not
            if (String.IsNullOrEmpty(textBlock_dcsWidth.Text) && (!String.IsNullOrEmpty(textBlock_dcsHeight.Text)))
            {
                System.Windows.MessageBox.Show("Enter a DCS Width Override or remove the DCS Height Override.");
                return;
            }

            if (String.IsNullOrEmpty(textBlock_dcsHeight.Text) && (!String.IsNullOrEmpty(textBlock_dcsWidth.Text)))
            {
                System.Windows.MessageBox.Show("Enter a DCS Height Override or remove the DCS Width Override.");
                return;
            }

            CreateShortcut();
        }

        private void button_userOptionsNewFile_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".lua";
            dlg.Filter = "lua files (*.lua)|*.lua"; //pick a lua only

            string userName = System.Environment.UserName;
            dlg.Title = "Example: " + @"C:\Users\" + userName +
                        @"\Saved Games\DCS.openbeta\Config\OptionsPresets\HighSettings.lua";


            string userOptionsStableLocation = @"C:\Users\" + userName + @"\Saved Games\DCS\Config";
            string userOptionsBetaLocation = @"C:\Users\" + userName + @"\Saved Games\DCS.openbeta\Config";
            string userOptionsAlphaLocation = @"C:\Users\" + userName + @"\Saved Games\DCS.openalpha\Config";

            //these are to assist the user in locating their correct directories. 
            //for the first one here, if there is something already in the text block, assume the user wants to 
            //access the same folder, so set the initial directory there. Otherwise, try the other folders
            if (!String.IsNullOrEmpty(textBlock_userOptionsNewFile.Text)
                && Directory.Exists(System.IO.Path.GetDirectoryName(textBlock_userOptionsNewFile.Text)))
            {
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName(textBlock_userOptionsNewFile.Text);
            }
            else if (Directory.Exists(userOptionsStableLocation))
            {
                dlg.InitialDirectory = userOptionsStableLocation;
            }
            else if (Directory.Exists(userOptionsBetaLocation))
            {
                dlg.InitialDirectory = userOptionsBetaLocation;
            }
            else if (Directory.Exists(userOptionsAlphaLocation))
            {
                dlg.InitialDirectory = userOptionsAlphaLocation;
            }
            else
            {
                dlg.InitialDirectory = @"C:\Users\" + userName + @"\Saved Games";
            }

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                textBlock_userOptionsNewFile.Text = filename;
                checkBox_updateOptions.IsEnabled = true;
            }
            else //this serves as a way to "clear" the text box. press cancel
            {
                textBlock_userOptionsNewFile.Text = null;
                checkBox_updateOptions.IsEnabled = false;
                checkBox_updateOptions.IsChecked = false;
            }
        }

        //https://stackoverflow.com/questions/42270283/how-disallow-space-blank-charachter-from-text-box-input-by-keyboard
        private void textBlock_dcsWidth_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void textBlock_dcsHeight_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        //https://stackoverflow.com/questions/938145/make-wpf-textbox-as-cut-copy-and-paste-restricted
        private void textBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if ( //e.Command == ApplicationCommands.Copy ||
                //e.Command == ApplicationCommands.Cut ||
                e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        private void button_userIconLocation_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".ico";
            dlg.Filter = "ico files (*.ico)|*.ico"; //pick an ico only
            dlg.Title = "Example: " + @"C:\Program Files\Eagle Dynamics\DCS World\FUI\DCS-1.ico";

            //a guess for the default install location
            if (!String.IsNullOrEmpty(textBlock_userIconLocation.Text)
                && Directory.Exists(System.IO.Path.GetDirectoryName(textBlock_userIconLocation.Text)))
            {
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName(textBlock_userIconLocation.Text);
            }
            else
            {
                dlg.InitialDirectory = @"C:\Program Files\Eagle Dynamics\DCS World\FUI";
            }


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                textBlock_userIconLocation.Text = filename;
            }
            else //this serves as a way to "clear" the text box. press cancel
            {
                textBlock_userIconLocation.Text = null;
            }
        }
    }
}
