using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selenium.Core.Helper;

public static class WebDriverWaitExtensions
{
    public static IWebElement AnyOf(this WebDriverWait wait, params Func<IWebDriver, IWebElement>[] conditions)
    {
        return wait.Until(driver =>
        {
            foreach (var condition in conditions)
            {
                try
                {
                    IWebElement element = condition(driver);
                    if (element != null)
                    {
                        return element;
                    }
                }
                catch{}
            }
            return null; // Không tìm thấy điều kiện nào thỏa mãn
        });
    }

    public static bool InvisibilityOfElementLocated(this IWebElement element)
    {
        try
        {
            return !element.Displayed;
        }
        catch
        {
            return true;
        }
    }
}
