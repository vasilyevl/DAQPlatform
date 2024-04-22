using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LV.Common
{
    /// <summary>
    /// Per MSDN article, 
    /// https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/enumeration-classes-over-enum-types
    /// </summary>

    public abstract class EnumBase : IComparable
    {
        protected EnumBase(string name, int id)
        {
            Id = id;
            Name = name;
        }

        public string Name { get; private set; }

        public int Id { get; private set; }

        public override string ToString() => Name;

        public static explicit operator int(EnumBase a) => a.Id;

        public static IEnumerable<T> GetAll<T>() where T : EnumBase
        {
            return typeof(T).GetFields( BindingFlags.Public 
                                        | BindingFlags.Static 
                                        | BindingFlags.DeclaredOnly)
                            .Select(f => f.GetValue(null)).Cast<T>();
        }

        public static List<string> GetAllNames<T>() where T : EnumBase
        {
            var items =  EnumBase.GetAll<T>();
            var names = new List<string>();

            if (items.Count() > 0) {
                foreach (var item in items) {
                    names.Add(item.Name);
                }
            }
            return names;
        }

        public static List<int> GetAllIndices<T>() where T : EnumBase
        {
            var items =  EnumBase.GetAll<T>();
            var indices = new List<int>();

            if (items.Count() > 0) {
                foreach (var item in items) {
                    indices.Add(item.Id);
                }
            }
            return indices;
        }

        public bool Equals(EnumBase other)
        {
            if (other is null) { return false; }

            return Id == other.Id && 
                string.Equals( Name, other.Name, 
                               StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) 
        {
            if ( obj == null) { return false; }

            var other  = obj as EnumBase;

            if (other is null) { return false; }
            
            return Equals (other);
        }

        public static bool operator == (EnumBase a, EnumBase b)
        {
            if (((object)a) is null || ((object)b) is null) {
                return Object.Equals(a, b);
            }
            return a.Equals(b);
        }

        public static bool operator !=(EnumBase a, EnumBase b)
        {
            if (((object)a) is null || ((object)b) is null) {
                return !Object.Equals(a, b);
            }
            return !a.Equals(b);
        }

        public override int GetHashCode() => 
            Name.GetHashCode() + Id.GetHashCode();

        public int CompareTo(object other) =>
            Id.CompareTo(((EnumBase)other).Id);

        public static T FromId<T>(int value) where T : EnumBase
        {
            var matchingItem = Parse<T, int>(value, "value", 
                                     item => item.Id == value);
            return matchingItem;
        }

        public static T FromName<T>(string name) where T : EnumBase
        {
            var matchingItem = Parse<T, string>(name, "value", 
                item => String.Equals( item.Name, name, 
                                       StringComparison.OrdinalIgnoreCase));
            return matchingItem;
        }

        private static T Parse<T, K>(K value, string description, 
                                Func<T, bool> predicate) where T : EnumBase
        {
            var matchingItem = GetAll<T>().FirstOrDefault(predicate);
            if (matchingItem == null) {
                throw new ApplicationException( $"\"{value}\" is not " +
                    $"a valid {description} in {typeof(T)}");
            }
            return matchingItem;
        }
    }
}
