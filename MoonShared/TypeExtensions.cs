using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace MoonShared
{
    public static class TypeExtensions
    {

        /// <summary>
        /// Apparently, in .NET Core, a hash code for a given string will be different between runs.
        /// https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/
        /// This gets one that will be the same.
        /// </summary>
        /// <param name="str">The string to get the hash code of.</param>
        /// <returns>The deterministic hash code.</returns>
        public static int GetDeterministicHashCode(this string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }


        /// <summary>Gets a method and asserts that it was found.</summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <param name="name">The method name.</param>
        /// <param name="parameters">The method parameter types, or <c>null</c> if it's not overloaded.</param>
        /// <returns>The corresponding <see cref="MethodInfo"/>, if found.</returns>
        /// <exception cref="MissingMethodException">If a matching method is not found.</exception>
        [DebuggerStepThrough]
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public static MethodInfo RequireMethod(this Type type, string name, Type[]? parameters)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        {
            return AccessTools.Method(type, name, parameters);
        }

        /// <summary>Get a value from an array if it's in range.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="array">The array to search.</param>
        /// <param name="index">The index of the value within the array to find.</param>
        /// <param name="value">The value at the given index, if found.</param>
        /// <returns>Returns whether the index was within the array bounds.</returns>
        public static bool TryGetIndex<T>(this T[] array, int index, out T value)
        {
            if (array == null || index < 0 || index >= array.Length)
            {
                value = default;
                return false;
            }

            value = array[index];
            return true;
        }

        /// <summary>Get a value from an array if it's in range, else get the default value.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="array">The array to search.</param>
        /// <param name="index">The index of the value within the array to find.</param>
        /// <param name="defaultValue">The default value if the value isn't in range.</param>
        public static T GetOrDefault<T>(this T[] array, int index, T defaultValue = default)
        {
            return array.TryGetIndex(index, out T value)
                ? value
                : defaultValue;
        }



        /// <summary>Shuffle a List for a random value.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="list">The list to be shuffled.</param>
        /// <param name="random">The RNG to shuffle off of.</param>
        public static void Shuffle<T>(this IList<T> list, Random random)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
