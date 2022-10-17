using System;
using System.Linq;
using PepperDash.Core;

namespace PepperDashRoomOs.Core
{
    public static class Extensions
    {
        public static int ParseContactMethodIndex(this PhonebookSearchContactResult contact, string response)
        {
            try
            {
                var stringToRemove = string.Format("{0}{1} ContactMethod ", RoomOsAddressBook.PhonebookContactResultStart, contact.Index);
                var result = response.Remove(0, stringToRemove.Length).Split(new[] { ' ' })[0];
                return Int32.Parse(result);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static PhonebookSearchContactResult ParseContactName(this PhonebookSearchContactResult contact, string response)
        {
            var stringToRemove = string.Format("{0}{1} Name: ", RoomOsAddressBook.PhonebookContactResultStart, contact.Index);
            contact.Name = response.Remove(0, stringToRemove.Length).TrimStart(new[] { '"' }).TrimEnd(new[] { '"' });
            return contact;
        }

        public static PhonebookSearchContactResult ParseContactId(this PhonebookSearchContactResult contact, string response)
        {
            var stringToRemove = string.Format("{0}{1} ContactId: ", RoomOsAddressBook.PhonebookContactResultStart, contact.Index);
            contact.ContactId = response.Remove(0, stringToRemove.Length).TrimStart(new[] { '"' }).TrimEnd(new[] { '"' });
            return contact;
        }

        public static PhonebookSearchResultContactMethod ParseContactMethodId(this PhonebookSearchResultContactMethod method, string response, PhonebookSearchContactResult contact)
        {
            var stringToRemove = string.Format("{0}{1} ContactMethod {2} ContactMethodId: ", RoomOsAddressBook.PhonebookContactResultStart, contact.Index, method.Index);
            method.ContactMethodId = response.Remove(0, stringToRemove.Length).TrimStart(new[] { '"' }).TrimEnd(new[] { '"' });
            return method;
        }

        public static PhonebookSearchResultContactMethod ParseContactMethodNumber(this PhonebookSearchResultContactMethod method, string response, PhonebookSearchContactResult contact)
        {
            var stringToRemove = string.Format("{0}{1} ContactMethod {2} Number: ", RoomOsAddressBook.PhonebookContactResultStart, contact.Index, method.Index);
            method.Number = response.Remove(0, stringToRemove.Length).TrimStart(new[] { '"' }).TrimEnd(new[] { '"' });
            return method;
        }

        public static PhonebookSearchContactResult ParseContactMethod(this PhonebookSearchContactResult contact, string response, uint debugLevel)
        {
            var index = contact.ParseContactMethodIndex(response);
            if (index == 0)
                throw new ArgumentOutOfRangeException("Contact Index", "Contact index is 0 so something is incorrect");

            var method = contact.ContactMethods.FirstOrDefault(s => s.Index == index);
            if (method == null)
            {
                method = new PhonebookSearchResultContactMethod { Index = index };
                contact.ContactMethods.Add(method);
            }

            if (!response.Contains("ContactMethod " + method.Index))
                throw new ArgumentOutOfRangeException("Method Index", "Method index is 0 so something is incorrect");

            if (response.Contains("ContactMethodId:"))
            {
                method.ParseContactMethodId(response, contact);
            }
            else if (response.Contains("Number:"))
            {
                method.ParseContactMethodNumber(response, contact);
            }
            else
            {
                Debug.Console(debugLevel, "Not sure what to do with this contact method response:{0}", response);
            }

            return contact;
        }
    }
}