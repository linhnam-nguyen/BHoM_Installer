/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using System.Globalization;
using WixToolset.Dtf.WindowsInstaller;

namespace InstallerCA
{
    public class CustomAction
    {
        #region Methods

        #region CaRegisterAddIn
        [CustomAction]
        public static ActionResult CaRegisterAddIn(Session session)
        {
            string szOfficeRegKeyVersions = string.Empty;
            string szBaseAddInKey = @"Software\Microsoft\Office\";
            string szXll32Bit = string.Empty;
            string szXll64Bit = string.Empty;
            string szXllToRegister = string.Empty;
            string szFolder = string.Empty;
            int nOpenVersion;
            double nVersion;
            bool bFoundOffice = false;
            List<string> lstVersions;

            try
            {
                session.Log("DIAGNOSTICS: Starting CaRegisterAddIn");
                session.Log("DIAGNOSTICS: User: {0}", Environment.UserName);
                session.Log("DIAGNOSTICS: OS Bitness: {0}", Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");

                szOfficeRegKeyVersions = session.CustomActionData["OFFICEREGKEYS"];
                szXll32Bit = session.CustomActionData["XLL32"];
                szXll64Bit = session.CustomActionData["XLL64"];
                szFolder = session.CustomActionData["FOLDER"];

                session.Log("DIAGNOSTICS: OfficeRegKeys from MSI: {0}", szOfficeRegKeyVersions);
                session.Log("DIAGNOSTICS: Target Folder: {0}", szFolder);

                szXll32Bit = szFolder.ToString() + szXll32Bit.ToString();
                szXll64Bit = szFolder.ToString() + szXll64Bit.ToString();

                if (szOfficeRegKeyVersions.Length > 0)
                {
                    lstVersions = szOfficeRegKeyVersions.Split(',').ToList();

                    foreach (string szOfficeVersionKey in lstVersions)
                    {
                        nVersion = double.Parse(szOfficeVersionKey, NumberStyles.Any, CultureInfo.InvariantCulture);

                        string fullOfficeKey = szBaseAddInKey + szOfficeVersionKey;
                        session.Log("DIAGNOSTICS: Checking Office key: HKCU\\{0}", fullOfficeKey);

                        // get the OPEN keys from the Software\Microsoft\Office\[Version]\Excel\Options key, skip if office version not found.
                        using (RegistryKey? rkOffice = Registry.CurrentUser?.OpenSubKey(fullOfficeKey, false))
                        {
                            if (rkOffice != null)
                            {
                                string szKeyName = fullOfficeKey + @"\Excel\Options";
                                session.Log("DIAGNOSTICS: Office {0} found. Accessing options: {1}", szOfficeVersionKey, szKeyName);

                                szXllToRegister = GetAddInName(session, szXll32Bit, szXll64Bit, szOfficeVersionKey, nVersion);
                                session.Log("DIAGNOSTICS: Resolved XLL to register: {0}", szXllToRegister);

                                using (RegistryKey? rkExcelXll = Registry.CurrentUser?.OpenSubKey(szKeyName, true))
                                {
                                    if (szXllToRegister != string.Empty && rkExcelXll != null)
                                    {
                                        string[] szValueNames = rkExcelXll.GetValueNames();
                                        bool bIsOpen = false;
                                        int nMaxOpen = -1;

                                        // check every value for OPEN keys
                                        foreach (string szValueName in szValueNames)
                                        {
                                            if (szValueName.StartsWith("OPEN"))
                                            {
                                                string suffix = szValueName.Length > 4 ? szValueName.Substring(4) : "0";
                                                int nNewOpen = int.TryParse(suffix, NumberStyles.Any, CultureInfo.InvariantCulture, out nOpenVersion) ? nOpenVersion : 0;
                                                
                                                if (nNewOpen > nMaxOpen)
                                                {
                                                    nMaxOpen = nNewOpen;
                                                }

                                                var val = rkExcelXll.GetValue(szValueName);
                                                session.Log("DIAGNOSTICS: Existing {0} value: {1}", szValueName, val);

                                                if (val != null && val.ToString()?.ToLower().Contains(szXllToRegister.ToLower()) == true)
                                                {
                                                    session.Log("DIAGNOSTICS: BHoM is already registered in {0}", szValueName);
                                                    bIsOpen = true;
                                                }
                                            }
                                        }

                                        // if adding a new key
                                        if (!bIsOpen)
                                        {
                                            string newOpenName = nMaxOpen == -1 ? "OPEN" : "OPEN" + (nMaxOpen + 1).ToString();
                                            string newValue = "/R \"" + szXllToRegister + "\"";
                                            session.Log("DIAGNOSTICS: Registering BHoM as {0} with value {1}", newOpenName, newValue);
                                            rkExcelXll.SetValue(newOpenName, newValue);
                                        }
                                        bFoundOffice = true;
                                    }
                                    else
                                    {
                                        session.Log("ERROR: Unable to open Excel Options key for write access or XLL name empty.");
                                    }
                                }
                            }
                            else
                            {
                                session.Log("DIAGNOSTICS: Office version {0} not found in registry, skipping.", szOfficeVersionKey);
                            }
                        }
                    }
                }

                session.Log("DIAGNOSTICS: CaRegisterAddIn finished successfully.");
            }
            catch (Exception ex)
            {
                session.Log("FATAL ERROR in CaRegisterAddIn: " + ex.ToString());
                bFoundOffice = false;
            }

            return bFoundOffice ? ActionResult.Success : ActionResult.Failure;
        }
        #endregion

        #region CaUnRegisterAddIn
        [CustomAction]
        public static ActionResult CaUnRegisterAddIn(Session session)
        {
            string szOfficeRegKeyVersions = string.Empty;
            string szBaseAddInKey = @"Software\Microsoft\Office\";
            string szXll32Bit = string.Empty;
            string szXll64Bit = string.Empty;
            string szFolder = string.Empty;
            bool bFoundOffice = false;
            List<string> lstVersions;

            try
            {
                session.Log("DIAGNOSTICS: Starting CaUnRegisterAddIn");

                szOfficeRegKeyVersions = session.CustomActionData["OFFICEREGKEYS"];
                szXll32Bit = session.CustomActionData["XLL32"];
                szXll64Bit = session.CustomActionData["XLL64"];
                szFolder = session.CustomActionData["FOLDER"];

                szXll32Bit = szFolder.ToString() + szXll32Bit.ToString();
                szXll64Bit = szFolder.ToString() + szXll64Bit.ToString();


                if (szOfficeRegKeyVersions.Length > 0)
                {
                    lstVersions = szOfficeRegKeyVersions.Split(',').ToList();

                    foreach (string szOfficeVersionKey in lstVersions)
                    {
                        using (RegistryKey? rkOffice = Registry.CurrentUser?.OpenSubKey(szBaseAddInKey + szOfficeVersionKey, false))
                        {
                            if (rkOffice != null)
                            {
                                bFoundOffice = true;
                                string szKeyName = szBaseAddInKey + szOfficeVersionKey + @"\Excel\Options";

                                using (RegistryKey? rkAddInKey = Registry.CurrentUser?.OpenSubKey(szKeyName, true))
                                {
                                    if (rkAddInKey != null)
                                    {
                                        string[] szValueNames = rkAddInKey.GetValueNames();

                                        foreach (string szValueName in szValueNames)
                                        {
                                            if (szValueName.StartsWith("OPEN"))
                                            {
                                                var val = rkAddInKey.GetValue(szValueName);
                                                if (val != null && (val.ToString()?.Contains(szXll32Bit) == true || val.ToString()?.Contains(szXll64Bit) == true))
                                                {
                                                    session.Log("DIAGNOSTICS: Unregistering BHoM from {0}", szValueName);
                                                    rkAddInKey.DeleteValue(szValueName);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                session.Log("DIAGNOSTICS: CaUnRegisterAddIn finished.");
            }
            catch (Exception ex)
            {
                session.Log("ERROR in CaUnRegisterAddIn: " + ex.ToString());
            }

            return bFoundOffice ? ActionResult.Success : ActionResult.Failure;
        }
        #endregion

        #region ClosePrompt
        [CustomAction]
        public static ActionResult ClosePrompt(Session session)
        {
            session.Log("DIAGNOSTICS: Starting ClosePrompt");
            try
            {
                var productName = session["ProductName"];
                var processesProp = session["PromptToCloseProcesses"];
                var displayNamesProp = session["PromptToCloseDisplayNames"];

                if (string.IsNullOrEmpty(processesProp) || string.IsNullOrEmpty(displayNamesProp))
                {
                    session.Log("DIAGNOSTICS: No processes specified to close.");
                    return ActionResult.Success;
                }

                var processes = processesProp.Split(',');
                var displayNames = displayNamesProp.Split(',');

                if (processes.Length != displayNames.Length)
                {
                    session.Log("ERROR: Mismatch between PromptToCloseProcesses ({0}) and PromptToCloseDisplayNames ({1})", processes.Length, displayNames.Length);
                    return ActionResult.Failure;
                }

                for (var i = 0; i < processes.Length; i++)
                {
                    session.Log("DIAGNOSTICS: Checking if process {0} ({1}) is running.", processes[i], displayNames[i]);
                    using (var prompt = new PromptCloseApplication(productName, processes[i], displayNames[i]))
                    {
                        if (!prompt.Prompt())
                        {
                            session.Log("DIAGNOSTICS: User cancelled close prompt for {0}.", processes[i]);
                            return ActionResult.Failure;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                session.Log("FATAL ERROR in ClosePrompt: " + ex.ToString());
                return ActionResult.Failure;
            }

            session.Log("DIAGNOSTICS: ClosePrompt finished successfully.");
            return ActionResult.Success;
        }
        #endregion

        #region GetAddInName
        public static string GetAddInName(Session session, string szXll32Name, string szXll64Name, string szOfficeVersionKey, double nVersion)
        {
            string szXllToRegister = string.Empty;

            if (nVersion >= 14)
            {
                // determine if office is 32-bit or 64-bit
                RegistryKey localMachineRegistry = // 64bit machines need to determine correct hive.
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                        Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);
                
                string outlookKeyPath = @"Software\Microsoft\Office\" + szOfficeVersionKey + @"\Outlook";
                using (RegistryKey? rkBitness = localMachineRegistry.OpenSubKey(outlookKeyPath, false))
                {
                    if (rkBitness != null)
                    {
                        object? oBitValue = rkBitness.GetValue("Bitness");
                        session.Log("DIAGNOSTICS: Found Outlook Bitness key: {0}", oBitValue);
                        if (oBitValue != null && oBitValue.ToString() == "x64")
                        {
                            szXllToRegister = szXll64Name;
                        }
                        else
                        {
                            szXllToRegister = szXll32Name;
                        }
                    }
                    else
                    {
                        if (Environment.Is64BitOperatingSystem)
                        {
                            session.Log("DIAGNOSTICS: Outlook key not found in main hive, checking 32-bit hive.");
                            using (RegistryKey localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                            using (RegistryKey? rkBitness32 = localMachine32.OpenSubKey(outlookKeyPath, false))
                            {
                                if (rkBitness32 != null)
                                {
                                    var oBitValue = rkBitness32.GetValue("Bitness");
                                    session.Log("DIAGNOSTICS: Found Outlook Bitness (32-bit hive): {0}", oBitValue);
                                    if (oBitValue != null && oBitValue.ToString() == "x64")
                                    {
                                        szXllToRegister = szXll64Name;
                                    }
                                    else
                                    {
                                        szXllToRegister = szXll32Name;
                                    }
                                }
                                else
                                {
                                    session.Log("DIAGNOSTICS: Outlook key not found in 32-bit hive either. Defaulting to 32-bit XLL.");
                                    szXllToRegister = szXll32Name;
                                }
                            }
                        }
                        else
                        {
                            session.Log("DIAGNOSTICS: Outlook bitness key not found. Defaulting to 32-bit XLL.");
                            szXllToRegister = szXll32Name;
                        }
                    }
                }
            }
            else
            {
                session.Log("DIAGNOSTICS: Office version < 14. Defaulting to 32-bit XLL.");
                szXllToRegister = szXll32Name;
            }

            return szXllToRegister;
        }
        #endregion
        #endregion
    }
}
