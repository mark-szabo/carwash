using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace MSHU.CarWash.DomainModel.Helpers
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum element)
        {
            Type type = element.GetType();

            MemberInfo[] memberInfo = type.GetMember(element.ToString());

            if (memberInfo != null && memberInfo.Length > 0)
            {
                var attributes = memberInfo[0].GetCustomAttributes(typeof(DisplayAttribute), false);

                if (attributes != null && attributes.Count() > 0)
                {
                    return ((DisplayAttribute)attributes.First()).Description;
                }
            }

            return element.ToString();
        }

    }
}