/*
Copyright (c) 2024 vasilyevl (Grumpy). Permission is hereby granted, 
free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"),to deal in the Software 
without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the 
Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,FITNESS FOR A 
PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Reflection;

namespace Grumpy.SDAQFramework.Common
{
    /// <summary>
    /// Base class for creating enum-like classes with additional functionality.
    /// </summary>
    /// <remarks>
    /// This class is based on the MSDN article:
    /// https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/enumeration-classes-over-enum-types
    /// </remarks>

    public abstract class EnumBase : IComparable
    {
        private readonly string _name;
        private readonly int _id;


        /// <summary>
        /// Initializes a new instance of the <see cref="EnumBase"/> class.
        /// </summary>
        /// <param name="name">The name of the enum value.</param>
        /// <param name="id">The ID of the enum value.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
        protected EnumBase(string name, int id) {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _id = id;
        }


        /// <summary>
        /// Gets the name of the enum value.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Gets the ID of the enum value.
        /// </summary>
        public int Id => _id;

        /// <summary>
        /// Returns a string representation of the enum value.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() => $"{Name} ({Id})";

        /// <summary>
        /// Explicitly converts an <see cref="EnumBase"/> to an <see cref="int"/>.
        /// </summary>
        /// <param name="a">The <see cref="EnumBase"/> instance.</param>
        public static explicit operator int(EnumBase a) => a.Id;

        /// <summary>
        /// Gets all items of the specified enum type.
        /// </summary>
        /// <typeparam name="T">The type of the enum.</typeparam>
        /// <returns>A list of all items of the specified enum type.</returns>
        public static List<T> AllItems<T>() where T : EnumBase =>
            typeof(T).GetFields(BindingFlags.Public 
                                | BindingFlags.Static 
                                | BindingFlags.DeclaredOnly)
                     .Select(f => f.GetValue(null))
                     .Cast<T>()
                     .ToList();

        /// <summary>
        /// Gets all names of the specified enum type.
        /// </summary>
        /// <typeparam name="T">The type of the enum.</typeparam>
        /// <returns>A list of all names of the specified enum type.</returns>
        public static List<string> AllNames<T>() where T : EnumBase =>
            AllItems<T>().Select(item => item.Name).ToList();

        /// <summary>
        /// Gets all IDs of the specified enum type.
        /// </summary>
        /// <typeparam name="T">The type of the enum.</typeparam>
        /// <returns>A list of all IDs of the specified enum type.</returns>
        public static List<int> AllIds<T>() where T : EnumBase =>
            AllItems<T>().Select(item => item.Id).ToList();

        /// <summary>
        /// Determines whether the specified <see cref="EnumBase"/> is equal to the current <see cref="EnumBase"/>.
        /// </summary>
        /// <param name="other">The <see cref="EnumBase"/> to compare with the current <see cref="EnumBase"/>.</param>
        /// <returns>true if the specified <see cref="EnumBase"/> is equal to the current <see cref="EnumBase"/>; otherwise, false.</returns>
        public bool Equals(EnumBase other) =>
            other != null! 
            && Id == other.Id 
            && string.Equals(Name, 
                other.Name, 
                StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object? obj) =>
            obj is EnumBase other && Equals(other);

        /// <summary>/// <summary>
        /// Determines whether two specified instances of <see cref="EnumBase"/> are equal.
        /// </summary>
        /// <param name="a">The first <see cref="EnumBase"/> to compare.</param>
        /// <param name="b">The second <see cref="EnumBase"/> to compare.</param>
        /// <returns>true if the two <see cref="EnumBase"/> instances are equal; otherwise, false.</returns>
        public static bool operator ==(EnumBase a, EnumBase b) =>
            a is null ? b is null : a.Equals(b);

        /// <summary>
        /// Determines whether two specified instances of <see cref="EnumBase"/> are not equal.
        /// </summary>
        /// <param name="a">The first <see cref="EnumBase"/> to compare.</param>
        /// <param name="b">The second <see cref="EnumBase"/> to compare.</param>
        /// <returns>true if the two <see cref="EnumBase"/> instances are not equal; otherwise, false.</returns>
        public static bool operator !=(EnumBase a, EnumBase b) =>
            !(a == b);

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() =>
            HashCode.Combine(Name, Id);


        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="obj">An object to compare with this object.</param>
        /// <returns>A value that indicates the relative order of the objects being compared.</returns>
        public int CompareTo(object? obj) =>
            obj is EnumBase other ? Id.CompareTo(other.Id) : 1;


        /// <summary>
        /// Gets an enum value of the specified type by ID.
        /// </summary>
        /// <typeparam name="T">The type of the enum.</typeparam>
        /// <param name="id">The ID of the enum value.</param>
        /// <returns>The enum value with the specified ID.</returns>
        public static T FromId<T>(int id) where T : EnumBase =>
            Parse<T, int>(id, "ID", match => match.Id == id);


        /// <summary>
        /// Gets an enum value of the specified type by name.
        /// </summary>
        /// <typeparam name="T">The type of the enum.</typeparam>
        /// <param name="name">The name of the enum value.</param>
        /// <returns>The enum value with the specified name.</returns>
        public static T FromName<T>(string name) where T : EnumBase =>
            Parse<T, string>(name, 
                "Name", 
                match => string.Equals(match.Name, 
                    name, 
                    StringComparison.OrdinalIgnoreCase));


        /// <summary>
        /// Parses and finds an enum value based on a criterion.
        /// </summary>
        /// <typeparam name="T">The type of the enum.</typeparam>
        /// <typeparam name="K">The type of the parameter value.</typeparam>
        /// <param name="parameterValue">The parameter value to search for.</param>
        /// <param name="parameterDescription">The description of the parameter.</param>
        /// <param name="criterion">The criterion to match.</param>
        /// <returns>The enum value that matches the criterion.</returns>
        /// <exception cref="ApplicationException">Thrown when no match is found.</exception>
        private static T Parse<T, K>(K parameterValue, 
            string parameterDescription, 
            Func<T, bool> criterion) where T : EnumBase {

            T? foundMatch = AllItems<T>().FirstOrDefault(criterion);
            
            if (foundMatch! == null!) {
                throw new ApplicationException($"\"{parameterValue}\" " +
                    $"is not a valid {parameterDescription} in {typeof(T)}");
            }
            return foundMatch;
        }
    }
}
