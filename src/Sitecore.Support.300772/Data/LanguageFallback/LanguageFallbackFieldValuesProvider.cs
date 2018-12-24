using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using Sitecore.Caching;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Engines.DataCommands;
using Sitecore.Data.Events;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.LanguageFallback;
using Sitecore.Data.Managers;
using Sitecore.Eventing;
using Sitecore.Eventing.Remote;
using Sitecore.Globalization;
using Sitecore.Sites;

namespace Sitecore.Support.Data.LanguageFallback
{
  public class LanguageFallbackFieldValuesProvider: Sitecore.Data.LanguageFallback.LanguageFallbackFieldValuesProvider
  {
    public override bool IsValidForFallback(Field field)
    {
      var switcherValue = LanguageFallbackFieldSwitcher.CurrentValue;
      if (switcherValue == false)
      {
        return false;
      }

      SiteContext site;
      if (switcherValue != true && ((site = Context.Site) == null || !site.SiteInfo.EnableFieldLanguageFallback))
      {
        return false;
      }

      bool result = true;

      var item = field.Item;
      var key = new IsLanguageFallbackValidCacheKey(item.ID.ToString(), field.ID.ToString(), field.Database.Name, field.Language.Name);
      var cachedResult = this.GetFallbackIsValidValueFromCache(field, key);
      if (cachedResult != null)
      {
        result = (string)cachedResult == "1";
        return result;
      }

      Language fallbackLanguage = LanguageFallbackManager.GetFallbackLanguage(item.Language, item.Database, item.ID);
      if (fallbackLanguage == null || string.IsNullOrEmpty(fallbackLanguage.Name))
      {
        result = false;
      }
      // shared fields cannot have language fallback by definition
      else if (field.Shared)
      {
        result = false;
      }
      else if (this.ShouldStandardFieldBeSkipped(field))
      {
        result = false;
      }
      else if (StandardValuesManager.IsStandardValuesHolder(item))
      {
        result = false;
      }
      else if (field.ID == FieldIDs.EnableLanguageFallback || field.ID == FieldIDs.EnableSharedLanguageFallback)
      {
        result = false;
      }
      else if (!field.SharedLanguageFallbackEnabled)
      {
        if (Settings.LanguageFallback.AllowVaryFallbackSettingsPerLanguage)
        {
          Item innerItem;
          using (new LanguageFallbackItemSwitcher(false))
          {
            innerItem = field.InnerItem;
          }

          if (innerItem == null || innerItem.Fields[FieldIDs.EnableLanguageFallback].GetValue(false, false) != "1")
          {
            result = false;
          }
        }
        else
        {
          result = false;
        }
      }

      this.AddFallbackIsValidValueToCache(field, key, result);
      return result;
    }
  }
}