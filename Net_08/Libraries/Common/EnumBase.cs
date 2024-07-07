using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.Common
{
    /// <summary>
    /// Per MSDN article, 
    /// https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/enumeration-classes-over-enum-types
    /// </summary>

    public abstract class EnumBase : IComparable
    {
        string? _name;
        int _id;

        protected EnumBase(string name, int id) 
        {
            Id = id;
            Name = name;
        }

        public string Name { 
            get => _name is null ? string.Empty : (string)_name.Clone(); 
            private set => _name = value is null ? null : (string)value.Clone(); 
        }

        public int Id {
            get => _id; 
            private set => _id = value; }

        public override string ToString() => $"{Name} ({Id})";

        public static explicit operator int(EnumBase a) => a.Id;

        public static List<T> GetAllItems<T>() where T : EnumBase => 
            typeof(T).GetFields( BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Select(f => f.GetValue(null)).Cast<T>()
                    .ToList();
        public static List<string> GetAllNames<T>() where T : EnumBase
        {
            List<T> itemsAsList =  EnumBase.GetAllItems<T>();
            return ((itemsAsList?.Count ?? 0) > 0) ? itemsAsList!.Select(item => item.Name).ToList() : [];
        }

        public static List<int> GetAllIndices<T>() where T : EnumBase
        {
            List<T> itemsAsList =  EnumBase.GetAllItems<T>();
            return (itemsAsList?.Count ?? 0) > 0 ? itemsAsList!.Select(item => item.Id).ToList() : [];
        }

        public bool Equals(EnumBase other) => other != null! && Id == other.Id && 
                string.Equals( Name, other.Name, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? o) => 
            (o != null) && o is EnumBase && Equals((EnumBase)o);

        public static bool operator == (EnumBase a, EnumBase b)  =>  
            (a is null || b is null) ? Object.Equals(a, b): a.Equals(b);

        public static bool operator != (EnumBase a, EnumBase b) =>
            ((object?)a is null || (object?)b is null) ? !Object.Equals(a, b): !a.Equals(b);
        
        public override int GetHashCode() =>  Name.GetHashCode() + Id.GetHashCode();

        public int CompareTo(object? comparable) => 
            (comparable is null) ? 1 :Id.CompareTo(((EnumBase)comparable).Id);
            
        public static T FromId<T>(int id) where T : EnumBase =>
            Parse<T, int>(id, "ID", match => match.Id == id);

        public static T FromName<T>(string name) where T : EnumBase =>
         Parse<T, string>(name, "Name", 
                match => String.Equals( match.Name, name, StringComparison.OrdinalIgnoreCase));   

        private static T Parse<T, K>(K parameterValue, string parameterDescription, 
                                Func<T, bool> criterion) where T : EnumBase
        {
            T foundMatch = GetAllItems<T>().FirstOrDefault(criterion)!;

            if (foundMatch == null!) {

                throw new ApplicationException( $"\"{parameterValue}\" is not " +
                    $"a valid {parameterDescription} in {typeof(T)}");
            }

            return foundMatch;
        }
    }
}
