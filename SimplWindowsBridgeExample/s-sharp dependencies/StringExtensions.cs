//-----------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="Corporate Initiatives Group Asia Pty Ltd">
//     http://www.thecigroup.com.au
//     All source code excluding third party packages remains the sole property of Corporate Initiatives Group Asia Pty Ltd.
//     Source code may not be implemented, extended, modified, copied, re-distributed or deployed
//     without the express written consent of an authorised employee of Corporate Initiatives Group Asia Pty Ltd.
//     For more details please refer to the LICENSE file located in the root folder of the project source code.
//     20221128 Rod Driscoll
//     e: rodney.driscoll@thecigroup.com.au
//     m: +61 2 9223 3955
//     {c} Licensed to orporate Initiatives Group Asia Pty Ltd 2022.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Text;
using Crestron.SimplSharp;
using System.Collections.Generic; 
using System.Linq;

namespace CI.SSharp.Strings
{
    /// <summary>
    /// From: https://help.crestron.com/simpl_sharp/#
    /// NOTE: SIMPL+ STRING arrays cannot be passed to your SIMPL# module..
    /// Here's how to do it, albeit very clunky!
    /// </summary>
    public class SimplSharpStringArray
    {
        private Dictionary<ushort, SimplSharpString> _dict;
        public SimplSharpString[] Array
        {
            get
            {
                //CrestronConsole.PrintLine("get SimplSharpStringArray");
                //var ordered_ = _dict.OrderBy(pair => pair.Key) as Dictionary<ushort, string>; // this can't be typecast to Dictionary
                SimplSharpString[] array_ = new SimplSharpString[Length];
                foreach (var kv in _dict)
                {
                    array_[kv.Key] = kv.Value;
                    //CrestronConsole.PrintLine("array_[{0}] = {1}", kv.Key, array_[kv.Key]);
                }
                return array_;
            }
            private set
            {
                //CrestronConsole.PrintLine("set SimplSharpStringArray");
                ushort i = 0;
                _dict = new Dictionary<ushort, SimplSharpString>();
                foreach (var item_ in value)
                {
                    _dict.Add(i, item_);
                    i++;
                    //CrestronConsole.PrintLine("_dict[{0}] = {1}", i, _dict[i]);
                }

            }
        }
        public ushort Length
        {
            get 
            {
                //if (_dict.Count == 0) CrestronConsole.PrintLine("Length: zero");
                if (_dict.Count == 0) return (ushort)0;
                //CrestronConsole.PrintLine("Length: {0}", 1 + _dict.Keys.Last());
                return (ushort)(1 + _dict.Keys.Last());
            }
        }

        public SimplSharpStringArray ()
	    {
            //CrestronConsole.PrintLine("created SimplSharpStringArray");
            Clear();
	    }

        public void Insert(ushort pos, string val)
        {
            //CrestronConsole.PrintLine("Insert [{0}]: {1}", pos, val);
            _dict[pos] = val;
            //CrestronConsole.PrintLine("Insert[{0}]: {1}", pos, _dict[pos]);
        }
        public void Clear()
        {
            _dict = new Dictionary<ushort, SimplSharpString>();
        }
    }


    public static class StringExtensions
    {
        private static void Test(SimplSharpStringArray strings)
        {
            CrestronConsole.PrintLine("StringExtensions Test");
            SimplSharpString[] array_ = strings.Array;
            CrestronConsole.PrintLine("array_.len: {0}", array_.Length);
            for (int i = 0; i < strings.Length; i++)
                CrestronConsole.PrintLine("array_[{0}]:{1}\n", i, array_[i]);
        }

        public static string StringFormat(string format, SimplSharpStringArray string_array, ushort[] ints)
        {
            //CrestronConsole.PrintLine("StringFormat");
            SimplSharpString[] strings_ = string_array.Array;
            var items = new List<object>();
            for (int i = 0; i < Math.Max(strings_.Length, ints.Length); i++)
            {
                if (i < strings_.Length && strings_[i] != null && !String.IsNullOrEmpty(strings_[i].ToString()))
                    items.Add(strings_[i].ToString());
                else if (i < ints.Length)
                    items.Add(ints[i]);
                //CrestronConsole.PrintLine("items([{0}]: {2}\n", i, items.Last());
            }
            try
            {
                //CrestronConsole.PrintLine("items.Count: {0}\n", items.Count);
                StringBuilder sb_ = new StringBuilder();
                foreach (var x in items)
                    sb_.Append(x.ToString()+",");
                sb_.Remove(sb_.Length-1, 1);
                //CrestronConsole.PrintLine("items: {{ {0} }}\n", sb_);

                string s = String.Format(format, items.ToArray());
                //CrestronConsole.PrintLine("result: {0}\n", s);
                return s;

            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("Exception in StringFormat: {0}", e.Message);
            }
            return format;
        }
    }
}
