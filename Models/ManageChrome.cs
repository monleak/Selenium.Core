﻿using CommunityToolkit.Mvvm.ComponentModel;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selenium.Core.Models;

public enum ManageChromeStatus
{
    Init,
    OpenChrome,
    Running,
    Stop,
    CloseChrome,
}

public partial class ManageChrome : ObservableRecipient
{
    public static readonly Dictionary<ManageChromeStatus, string> StatusDescriptions = new Dictionary<ManageChromeStatus, string>
    {
        { ManageChromeStatus.Init, "Profile creation successful" },
        { ManageChromeStatus.Running, "Work is running" },
        { ManageChromeStatus.Stop, "Work has stopped" },
        { ManageChromeStatus.CloseChrome, "Chrome window closed" },
        { ManageChromeStatus.OpenChrome, "Chrome window opened" },
    };

    public readonly string pathProfile;

    protected IWebDriver _driver;

    [ObservableProperty]
    private string displayname;

    private ManageChromeStatus status;

    [ObservableProperty]
    private string statusDescription;

    private int _PID;

    public ManageChrome(string pathProfile, string displayName)
    {
        this.pathProfile = pathProfile;
        Displayname = displayName;
        updateStatus(ManageChromeStatus.Init, true);
    }

    public void updateStatus(ManageChromeStatus newStatus, bool isUpdateStatusDescription = false)
    {
        status = newStatus;
        if (isUpdateStatusDescription)
        {
            StatusDescription = StatusDescriptions.GetValueOrDefault(newStatus, "Không xác định");
        }
    }

    public void updateStatusDescription()
    {
        StatusDescription = StatusDescriptions.GetValueOrDefault(status, "Không xác định");
    }

    public void InitChrome(Action<Action> executeInMainThread = null, string proxy = null)
    {
        if(_driver == null)
        {
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.EnableVerboseLogging = false;
            service.HideCommandPromptWindow = true;

            ChromeOptions options = new ChromeOptions();
            options.AddArgument($"--user-data-dir={pathProfile}");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-infobars");
            options.AddArgument("--disable-plugins");
            options.AddArgument("--disable-notifications");
            options.AddArgument("--disable-background-networking");
            options.AddArgument("--disable-default-apps");
            options.AddArgument("--disable-sync");
            options.AddArgument("--window-size=945,1012");

            if(!string.IsNullOrEmpty(proxy))
            {
                options.AddArgument($"--proxy-server={proxy}");
            }

            _driver = new ChromeDriver(service, options);
            _PID = service.ProcessId;
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1);
            executeInMainThread?.Invoke(() => updateStatus(ManageChromeStatus.OpenChrome, true));
        }
    }

    public void ExecuteAction(Func<IWebDriver, object, object> action, out object result, object paramForAction = null, Action<Action> executeInMainThread = null)
    {
        result = null;
        if (_driver is IWebDriver driver)
        {
            try 
            {
                executeInMainThread?.Invoke(() => updateStatus(ManageChromeStatus.Running, true));
                result = action(driver,paramForAction);
                executeInMainThread?.Invoke(() => updateStatus(ManageChromeStatus.Stop, true));
            }
            catch (NoSuchWindowException)
            {
                if (driver.WindowHandles.Count > 0)
                {
                    driver.SwitchTo().Window(driver.WindowHandles.Last());
                    executeInMainThread?.Invoke(() => updateStatus(ManageChromeStatus.Stop, true));
                }
                else
                {
                    QuitBrowser(executeInMainThread);
                }
            }
            catch(Exception)
            {
                executeInMainThread?.Invoke(() => updateStatus(ManageChromeStatus.Stop, true));
            }
        }
        else
        {
            executeInMainThread?.Invoke(() => updateStatus(ManageChromeStatus.CloseChrome, true));
        }
    }

    public void QuitBrowser(Action<Action> executeInMainThread = null, bool force = false)
    {
        if(status == ManageChromeStatus.Running && !force)
        {
            return;
        }

        try
        {
            if(_driver is IWebDriver driver)
            {
                driver.Quit();
                _driver = null;
            }
        }
        catch { }
        try
        {
            Process process = Process.GetProcessById(_PID);
            process.Kill();
        }
        catch { }
        if(executeInMainThread == null)
        {
            updateStatus(ManageChromeStatus.CloseChrome);
        }
        else
        {
            executeInMainThread?.Invoke(() => updateStatus(ManageChromeStatus.CloseChrome, true));
        }
    }

    private bool checkDriver()
    {
        try
        {
            if (_driver is IWebDriver driver)
            {
                driver.WindowHandles.Count();
                return true;
            }
        }
        catch { }

        return false;
    }

    public void Refresh()
    {
        if (status != ManageChromeStatus.Init && !checkDriver())
        {
            updateStatus(ManageChromeStatus.CloseChrome,true);
        }
        updateStatusDescription();
    }

    public bool CanStartWork()
    {
        Refresh();
        return status == ManageChromeStatus.OpenChrome || status == ManageChromeStatus.Stop;
    }

    public bool CanRemoveProfile()
    {
        if (!checkDriver())
        {
            updateStatus(ManageChromeStatus.CloseChrome);
        }

        return status == ManageChromeStatus.Init || status == ManageChromeStatus.CloseChrome;
    }
}