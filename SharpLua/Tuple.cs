using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace System
{

    public static class StringExtensionMethods
    {
        public static bool IsNullOrWhiteSpace(this string theString)
        {
            if (theString != null)
            {
                foreach (char c in theString)
                {
                    if (Char.IsWhiteSpace(c) == false)
                        return false;
                }
            }
            return true;
        }
    }

}

namespace SharpLua
{

    /// <summary>
    /// Utility class that simplifies cration of tuples by using
    /// method calls instead of constructor calls
    /// </summary>
    public static class Tuple
    {
        /// <summary>
        /// Creates a new tuple value with the specified elements. The method
        /// can be used without specifying the generic parameters, because C#
        /// compiler can usually infer the actual types.
        /// </summary>
        /// <param name="item1">First element of the tuple</param>
        /// <param name="second">Second element of the tuple</param>
        /// <returns>A newly created tuple</returns>
        public static Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 second)
        {
            return new Tuple<T1, T2>(item1, second);
        }

        /// <summary>
        /// Creates a new tuple value with the specified elements. The method
        /// can be used without specifying the generic parameters, because C#
        /// compiler can usually infer the actual types.
        /// </summary>
        /// <param name="item1">First element of the tuple</param>
        /// <param name="second">Second element of the tuple</param>
        /// <param name="third">Third element of the tuple</param>
        /// <returns>A newly created tuple</returns>
        public static Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 second, T3 third)
        {
            return new Tuple<T1, T2, T3>(item1, second, third);
        }

        /// <summary>
        /// Creates a new tuple value with the specified elements. The method
        /// can be used without specifying the generic parameters, because C#
        /// compiler can usually infer the actual types.
        /// </summary>
        /// <param name="item1">First element of the tuple</param>
        /// <param name="second">Second element of the tuple</param>
        /// <param name="third">Third element of the tuple</param>
        /// <param name="fourth">Fourth element of the tuple</param>
        /// <returns>A newly created tuple</returns>
        public static Tuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 second, T3 third, T4 fourth)
        {
            return new Tuple<T1, T2, T3, T4>(item1, second, third, fourth);
        }


        /// <summary>
        /// Extension method that provides a concise utility for unpacking
        /// tuple components into specific out parameters.
        /// </summary>
        /// <param name="tuple">the tuple to unpack from</param>
        /// <param name="ref1">the out parameter that will be assigned tuple.Item1</param>
        /// <param name="ref2">the out parameter that will be assigned tuple.Item2</param>
        public static void Unpack<T1, T2>(this Tuple<T1, T2> tuple, out T1 ref1, out T2 ref2)
        {
            ref1 = tuple.Item1;
            ref2 = tuple.Item2;
        }

        /// <summary>
        /// Extension method that provides a concise utility for unpacking
        /// tuple components into specific out parameters.
        /// </summary>
        /// <param name="tuple">the tuple to unpack from</param>
        /// <param name="ref1">the out parameter that will be assigned tuple.Item1</param>
        /// <param name="ref2">the out parameter that will be assigned tuple.Item2</param>
        /// <param name="ref3">the out parameter that will be assigned tuple.Item3</param>
        public static void Unpack<T1, T2, T3>(this Tuple<T1, T2, T3> tuple, out T1 ref1, out T2 ref2, T3 ref3)
        {
            ref1 = tuple.Item1;
            ref2 = tuple.Item2;
            ref3 = tuple.Item3;
        }

        /// <summary>
        /// Extension method that provides a concise utility for unpacking
        /// tuple components into specific out parameters.
        /// </summary>
        /// <param name="tuple">the tuple to unpack from</param>
        /// <param name="ref1">the out parameter that will be assigned tuple.Item1</param>
        /// <param name="ref2">the out parameter that will be assigned tuple.Item2</param>
        /// <param name="ref3">the out parameter that will be assigned tuple.Item3</param>
        /// <param name="ref4">the out parameter that will be assigned tuple.Item4</param>
        public static void Unpack<T1, T2, T3, T4>(this Tuple<T1, T2, T3, T4> tuple, out T1 ref1, out T2 ref2, T3 ref3, T4 ref4)
        {
            ref1 = tuple.Item1;
            ref2 = tuple.Item2;
            ref3 = tuple.Item3;
            ref4 = tuple.Item4;
        }
    }


    /// <summary>
    /// Represents a functional tuple that can be used to store
    /// two values of different types inside one object.
    /// </summary>
    /// <typeparam name="T1">The type of the first element</typeparam>
    /// <typeparam name="T2">The type of the second element</typeparam>
    public sealed class Tuple<T1, T2>
    {
        private readonly T1 item1;
        private readonly T2 item2;

        /// <summary>
        /// Retyurns the first element of the tuple
        /// </summary>
        public T1 Item1
        {
            get { return item1; }
        }

        /// <summary>
        /// Returns the second element of the tuple
        /// </summary>
        public T2 Item2
        {
            get { return item2; }
        }

        /// <summary>
        /// Create a new tuple value
        /// </summary>
        /// <param name="item1">First element of the tuple</param>
        /// <param name="second">Second element of the tuple</param>
        public Tuple(T1 item1, T2 item2)
        {
            this.item1 = item1;
            this.item2 = item2;
        }

        public override string ToString()
        {
            return string.Format("Tuple({0}, {1})", Item1, Item2);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (item1 == null ? 0 : item1.GetHashCode());
            hash = hash * 23 + (item2 == null ? 0 : item2.GetHashCode());
            return hash;
        }

        public override bool Equals(object o)
        {
            if (!(o is Tuple<T1, T2>))
            {
                return false;
            }

            var other = (Tuple<T1, T2>)o;

            return this == other;
        }

        public bool Equals(Tuple<T1, T2> other)
        {
            return this == other;
        }

        public static bool operator ==(Tuple<T1, T2> a, Tuple<T1, T2> b)
        {
            if (object.ReferenceEquals(a, null))
            {
                return object.ReferenceEquals(b, null);
            }
            if (a.item1 == null && b.item1 != null) return false;
            if (a.item2 == null && b.item2 != null) return false;
            return
                a.item1.Equals(b.item1) &&
                a.item2.Equals(b.item2);
        }

        public static bool operator !=(Tuple<T1, T2> a, Tuple<T1, T2> b)
        {
            return !(a == b);
        }

        public void Unpack(Action<T1, T2> unpackerDelegate)
        {
            unpackerDelegate(Item1, Item2);
        }
    }


    /// <summary>
    /// Represents a functional tuple that can be used to store
    /// two values of different types inside one object.
    /// </summary>
    /// <typeparam name="T1">The type of the first element</typeparam>
    /// <typeparam name="T2">The type of the second element</typeparam>
    /// <typeparam name="T3">The type of the third element</typeparam>
    public sealed class Tuple<T1, T2, T3>
    {
        private readonly T1 item1;
        private readonly T2 item2;
        private readonly T3 item3;

        /// <summary>
        /// Retyurns the first element of the tuple
        /// </summary>
        public T1 Item1
        {
            get { return item1; }
        }

        /// <summary>
        /// Returns the second element of the tuple
        /// </summary>
        public T2 Item2
        {
            get { return item2; }
        }

        /// <summary>
        /// Returns the second element of the tuple
        /// </summary>
        public T3 Item3
        {
            get { return item3; }
        }

        /// <summary>
        /// Create a new tuple value
        /// </summary>
        /// <param name="item1">First element of the tuple</param>
        /// <param name="second">Second element of the tuple</param>
        /// <param name="third">Third element of the tuple</param>
        public Tuple(T1 item1, T2 item2, T3 item3)
        {
            this.item1 = item1;
            this.item2 = item2;
            this.item3 = item3;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (item1 == null ? 0 : item1.GetHashCode());
            hash = hash * 23 + (item2 == null ? 0 : item2.GetHashCode());
            hash = hash * 23 + (item3 == null ? 0 : item3.GetHashCode());
            return hash;
        }

        public override bool Equals(object o)
        {
            if (!(o is Tuple<T1, T2, T3>))
            {
                return false;
            }

            var other = (Tuple<T1, T2, T3>)o;

            return this == other;
        }

        public static bool operator ==(Tuple<T1, T2, T3> a, Tuple<T1, T2, T3> b)
        {
            if (object.ReferenceEquals(a, null))
            {
                return object.ReferenceEquals(b, null);
            }
            if (a.item1 == null && b.item1 != null) return false;
            if (a.item2 == null && b.item2 != null) return false;
            if (a.item3 == null && b.item3 != null) return false;
            return
                a.item1.Equals(b.item1) &&
                a.item2.Equals(b.item2) &&
                a.item3.Equals(b.item3);
        }

        public static bool operator !=(Tuple<T1, T2, T3> a, Tuple<T1, T2, T3> b)
        {
            return !(a == b);
        }

        public void Unpack(Action<T1, T2, T3> unpackerDelegate)
        {
            unpackerDelegate(Item1, Item2, Item3);
        }
    }


    /// <summary>
    /// Represents a functional tuple that can be used to store
    /// two values of different types inside one object.
    /// </summary>
    /// <typeparam name="T1">The type of the first element</typeparam>
    /// <typeparam name="T2">The type of the second element</typeparam>
    /// <typeparam name="T3">The type of the third element</typeparam>
    /// <typeparam name="T4">The type of the fourth element</typeparam>
    public sealed class Tuple<T1, T2, T3, T4>
    {
        private readonly T1 item1;
        private readonly T2 item2;
        private readonly T3 item3;
        private readonly T4 item4;

        /// <summary>
        /// Retyurns the first element of the tuple
        /// </summary>
        public T1 Item1
        {
            get { return item1; }
        }

        /// <summary>
        /// Returns the second element of the tuple
        /// </summary>
        public T2 Item2
        {
            get { return item2; }
        }

        /// <summary>
        /// Returns the second element of the tuple
        /// </summary>
        public T3 Item3
        {
            get { return item3; }
        }

        /// <summary>
        /// Returns the second element of the tuple
        /// </summary>
        public T4 Item4
        {
            get { return item4; }
        }

        /// <summary>
        /// Create a new tuple value
        /// </summary>
        /// <param name="item1">First element of the tuple</param>
        /// <param name="second">Second element of the tuple</param>
        /// <param name="third">Third element of the tuple</param>
        /// <param name="fourth">Fourth element of the tuple</param>
        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            this.item1 = item1;
            this.item2 = item2;
            this.item3 = item3;
            this.item4 = item4;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (item1 == null ? 0 : item1.GetHashCode());
            hash = hash * 23 + (item2 == null ? 0 : item2.GetHashCode());
            hash = hash * 23 + (item3 == null ? 0 : item3.GetHashCode());
            hash = hash * 23 + (item4 == null ? 0 : item4.GetHashCode());
            return hash;
        }

        public override bool Equals(object o)
        {
            if (o.GetType() != typeof(Tuple<T1, T2, T3, T4>))
            {
                return false;
            }

            var other = (Tuple<T1, T2, T3, T4>)o;

            return this == other;
        }

        public static bool operator ==(Tuple<T1, T2, T3, T4> a, Tuple<T1, T2, T3, T4> b)
        {
            if (object.ReferenceEquals(a, null))
            {
                return object.ReferenceEquals(b, null);
            }
            if (a.item1 == null && b.item1 != null) return false;
            if (a.item2 == null && b.item2 != null) return false;
            if (a.item3 == null && b.item3 != null) return false;
            if (a.item4 == null && b.item4 != null) return false;
            return
                a.item1.Equals(b.item1) &&
                a.item2.Equals(b.item2) &&
                a.item3.Equals(b.item3) &&
                a.item4.Equals(b.item4);
        }

        public static bool operator !=(Tuple<T1, T2, T3, T4> a, Tuple<T1, T2, T3, T4> b)
        {
            return !(a == b);
        }

        public void Unpack(Action<T1, T2, T3, T4> unpackerDelegate)
        {
            unpackerDelegate(Item1, Item2, Item3, Item4);
        }
    }


}
