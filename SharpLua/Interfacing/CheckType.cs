using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace SharpLua
{
    /*
     * Type checking and conversion functions.
     *
     * Author: Fabio Mascarenhas
     * Version: 1.0
     */
    class CheckType
    {
        private ObjectTranslator translator;

        ExtractValue extractNetObject;
        ExtractValue extractNull;
        Dictionary<long, ExtractValue> extractValues = new Dictionary<long, ExtractValue>();

        public CheckType(ObjectTranslator translator)
        {
            this.translator = translator;

            extractValues.Add(typeof(object).TypeHandle.Value.ToInt64(), new ExtractValue(getAsObject));
            extractValues.Add(typeof(sbyte).TypeHandle.Value.ToInt64(), new ExtractValue(getAsSbyte));
            extractValues.Add(typeof(byte).TypeHandle.Value.ToInt64(), new ExtractValue(getAsByte));
            extractValues.Add(typeof(short).TypeHandle.Value.ToInt64(), new ExtractValue(getAsShort));
            extractValues.Add(typeof(ushort).TypeHandle.Value.ToInt64(), new ExtractValue(getAsUshort));
            extractValues.Add(typeof(int).TypeHandle.Value.ToInt64(), new ExtractValue(getAsInt));
            extractValues.Add(typeof(uint).TypeHandle.Value.ToInt64(), new ExtractValue(getAsUint));
            extractValues.Add(typeof(long).TypeHandle.Value.ToInt64(), new ExtractValue(getAsLong));
            extractValues.Add(typeof(ulong).TypeHandle.Value.ToInt64(), new ExtractValue(getAsUlong));
            extractValues.Add(typeof(double).TypeHandle.Value.ToInt64(), new ExtractValue(getAsDouble));
            extractValues.Add(typeof(char).TypeHandle.Value.ToInt64(), new ExtractValue(getAsChar));
            extractValues.Add(typeof(float).TypeHandle.Value.ToInt64(), new ExtractValue(getAsFloat));
            extractValues.Add(typeof(decimal).TypeHandle.Value.ToInt64(), new ExtractValue(getAsDecimal));
            extractValues.Add(typeof(bool).TypeHandle.Value.ToInt64(), new ExtractValue(getAsBoolean));
            extractValues.Add(typeof(string).TypeHandle.Value.ToInt64(), new ExtractValue(getAsString));
            extractValues.Add(typeof(LuaFunction).TypeHandle.Value.ToInt64(), new ExtractValue(getAsFunction));
            extractValues.Add(typeof(LuaTable).TypeHandle.Value.ToInt64(), new ExtractValue(getAsTable));
            extractValues.Add(typeof(LuaUserData).TypeHandle.Value.ToInt64(), new ExtractValue(getAsUserdata));
            
            extractNull = new ExtractValue(getNull);
            extractNetObject = new ExtractValue(getAsNetObject);
        }

        /*
         * Checks if the value at Lua stack index stackPos matches paramType,
         * returning a conversion function if it does and null otherwise.
         */
        internal ExtractValue getExtractor(IReflect paramType)
        {
            return getExtractor(paramType.UnderlyingSystemType);
        }
        internal ExtractValue getExtractor(Type paramType)
        {
            if (paramType.IsByRef) paramType = paramType.GetElementType();

            long runtimeHandleValue = paramType.TypeHandle.Value.ToInt64();

            if (extractValues.ContainsKey(runtimeHandleValue))
                return extractValues[runtimeHandleValue];
            else
                return extractNetObject;
        }

        internal ExtractValue checkType(SharpLua.Lua.LuaState luaState, int stackPos, Type paramType)
        {
            LuaTypes luatype = LuaDLL.lua_type(luaState, stackPos);

            if (paramType.IsByRef) paramType = paramType.GetElementType();

            Type underlyingType = Nullable.GetUnderlyingType(paramType);
            if (underlyingType != null)
            {
                paramType = underlyingType;     // Silently convert nullable types to their non null requics
            }

            long runtimeHandleValue = paramType.TypeHandle.Value.ToInt64();

            if (paramType.Equals(typeof(object)))
                return extractValues[runtimeHandleValue];

            //CP: Added support for generic parameters
            if (paramType.IsGenericParameter)
            {
                if (luatype == LuaTypes.LUA_TBOOLEAN)
                    return extractValues[typeof(bool).TypeHandle.Value.ToInt64()];
                else if (luatype == LuaTypes.LUA_TSTRING)
                    return extractValues[typeof(string).TypeHandle.Value.ToInt64()];
                else if (luatype == LuaTypes.LUA_TTABLE)
                    return extractValues[typeof(LuaTable).TypeHandle.Value.ToInt64()];
                else if (luatype == LuaTypes.LUA_TUSERDATA)
                    return extractValues[typeof(object).TypeHandle.Value.ToInt64()];
                else if (luatype == LuaTypes.LUA_TFUNCTION)
                    return extractValues[typeof(LuaFunction).TypeHandle.Value.ToInt64()];
                else if (luatype == LuaTypes.LUA_TNUMBER)
                    return extractValues[typeof(double).TypeHandle.Value.ToInt64()];
                //else // suppress CS0642
                ;//an unsupported type was encountered
            }
            
            if (LuaDLL.lua_isnil(luaState, stackPos))
            {
                return extractNull;
            }

            if (LuaDLL.lua_isnumber(luaState, stackPos))
                return extractValues[runtimeHandleValue];

            if (paramType == typeof(bool))
            {
                if (LuaDLL.lua_isboolean(luaState, stackPos))
                    return extractValues[runtimeHandleValue];
            }
            else if (paramType == typeof(string))
            {
                if (LuaDLL.lua_isstring(luaState, stackPos))
                    return extractValues[runtimeHandleValue];
                else if (luatype == LuaTypes.LUA_TNIL)
                    return extractNetObject; // kevinh - silently convert nil to a null string pointer
            }
            else if (paramType == typeof(char))
            // string -> char support
            {
                if (LuaDLL.lua_isstring(luaState, stackPos))
                {
                    string str = LuaDLL.lua_tostring(luaState, stackPos);
                    if (str.Length == 1) // must be char length (Length == 1)
                        return extractValues[runtimeHandleValue];
                }
                else if (luatype == LuaTypes.LUA_TNIL)
                    return extractNetObject;
            }
            else if (paramType == typeof(LuaTable))
            {
                if (luatype == LuaTypes.LUA_TTABLE)
                    return extractValues[runtimeHandleValue];
            }
            else if (paramType == typeof(LuaUserData))
            {
                if (luatype == LuaTypes.LUA_TUSERDATA)
                    return extractValues[runtimeHandleValue];
            }
            else if (paramType == typeof(LuaFunction))
            {
                if (luatype == LuaTypes.LUA_TFUNCTION)
                    return extractValues[runtimeHandleValue];
            }
            else if (typeof(Delegate).IsAssignableFrom(paramType) && luatype == LuaTypes.LUA_TFUNCTION)
            {
                return new ExtractValue(new DelegateGenerator(translator, paramType).extractGenerated);
            }
            else if (paramType.IsInterface && luatype == LuaTypes.LUA_TTABLE)
            {
                return new ExtractValue(new ClassGenerator(translator, paramType).extractGenerated);
            }
            else if ((paramType.IsInterface || paramType.IsClass) && luatype == LuaTypes.LUA_TNIL)
            {
                // kevinh - allow nil to be silently converted to null - extractNetObject will return null when the item ain't found
                return extractNetObject;
            }
            else if (LuaDLL.lua_type(luaState, stackPos) == LuaTypes.LUA_TTABLE)
            {
                if (LuaDLL.luaL_getmetafield(luaState, stackPos, "__index"))
                {
                    object obj = translator.getNetObject(luaState, -1);
                    LuaDLL.lua_settop(luaState, -2);
                    if (obj != null && paramType.IsAssignableFrom(obj.GetType()))
                        return extractNetObject;
                }
                else
                    return null;
            }
            else
            {
                object obj = translator.getNetObject(luaState, stackPos);
                if (obj != null && paramType.IsAssignableFrom(obj.GetType()))
                    return extractNetObject;
            }

            return null;
        }

        /*
         * The following functions return the value in the Lua stack
         * index stackPos as the desired type if it can, or null
         * otherwise.
         */
        private object getAsSbyte(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            sbyte retVal = (sbyte)LuaDLL.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaDLL.lua_isnumber(luaState, stackPos)) return null;
            return retVal;
        }
        private object getAsByte(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            byte retVal = (byte)LuaDLL.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaDLL.lua_isnumber(luaState, stackPos)) return null;
            return retVal;
        }
        private object getAsShort(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            short retVal = (short)LuaDLL.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaDLL.lua_isnumber(luaState, stackPos)) return null;
            return retVal;
        }
        private object getAsUshort(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            ushort retVal = (ushort)LuaDLL.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaDLL.lua_isnumber(luaState, stackPos)) return null;
            return retVal;
        }
        private object getAsInt(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            int retVal = (int)LuaDLL.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaDLL.lua_isnumber(luaState, stackPos)) return null;
            return retVal;
        }
        private object getAsUint(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            uint retVal = (uint)LuaDLL.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaDLL.lua_isnumber(luaState, stackPos)) return null;
            return retVal;
        }
        private object getAsLong(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            long retVal = (long)LuaDLL.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaDLL.lua_isnumber(luaState, stackPos)) return null;
            return retVal;
        }
        private object getAsUlong(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            ulong retVal = (ulong)LuaDLL.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaDLL.lua_isnumber(luaState, stackPos)) return null;
            return retVal;
        }
        private object getAsDouble(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            double retVal = LuaDLL.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaDLL.lua_isnumber(luaState, stackPos)) return null;
            return retVal;
        }
        private object getAsChar(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            if (LuaDLL.lua_isstring(luaState, stackPos))
            {
                string s = LuaDLL.lua_tostring(luaState, stackPos);
                if (s.Length == 0) // return a null char
                    return '\0';
                else
                {
                    if (s.Length > 1)
                        System.Diagnostics.Debug.WriteLine("String Length was greater than 1! Truncating...");
                    return s[0];
                }
            }
            char retVal = (char)LuaDLL.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaDLL.lua_isnumber(luaState, stackPos)) return null;
            return retVal;
        }
        private object getAsFloat(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            float retVal = (float)LuaDLL.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaDLL.lua_isnumber(luaState, stackPos)) return null;
            return retVal;
        }
        private object getAsDecimal(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            decimal retVal = (decimal)LuaDLL.lua_tonumber(luaState, stackPos);
            if (retVal == 0 && !LuaDLL.lua_isnumber(luaState, stackPos)) return null;
            return retVal;
        }
        private object getAsBoolean(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            return LuaDLL.lua_toboolean(luaState, stackPos);
        }
        private object getAsString(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            string retVal = LuaDLL.lua_tostring(luaState, stackPos);
            if (retVal == "" && !LuaDLL.lua_isstring(luaState, stackPos)) 
                return null;
            return retVal;
        }
        private object getAsTable(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            return translator.getTable(luaState, stackPos);
        }
        private object getAsFunction(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            return translator.getFunction(luaState, stackPos);
        }
        private object getAsUserdata(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            return translator.getUserData(luaState, stackPos);
        }
        public object getAsObject(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            if (LuaDLL.lua_type(luaState, stackPos) == LuaTypes.LUA_TTABLE)
            {
                if (LuaDLL.luaL_getmetafield(luaState, stackPos, "__index"))
                {
                    if (LuaDLL.luaL_checkmetatable(luaState, -1))
                    {
                        LuaDLL.lua_insert(luaState, stackPos);
                        LuaDLL.lua_remove(luaState, stackPos + 1);
                    }
                    else
                    {
                        LuaDLL.lua_settop(luaState, -2);
                    }
                }
            }
            object obj = translator.getObject(luaState, stackPos);
            return obj;
        }
        public object getAsNetObject(SharpLua.Lua.LuaState luaState, int stackPos)
        {
            object obj = translator.getNetObject(luaState, stackPos);
            if (obj == null && LuaDLL.lua_type(luaState, stackPos) == LuaTypes.LUA_TTABLE)
            {
                if (LuaDLL.luaL_getmetafield(luaState, stackPos, "__index"))
                {
                    if (LuaDLL.luaL_checkmetatable(luaState, -1))
                    {
                        LuaDLL.lua_insert(luaState, stackPos);
                        LuaDLL.lua_remove(luaState, stackPos + 1);
                        obj = translator.getNetObject(luaState, stackPos);
                    }
                    else
                    {
                        LuaDLL.lua_settop(luaState, -2);
                    }
                }
            }
            return obj;
        }
        
        public object getNull(Lua.LuaState luaState, int stackPos)
        {
            if (LuaDLL.lua_isnil(luaState, stackPos))
                return null;
            else
            {
                Debug.WriteLine("Value isn't nil!");
                return null;
            }
        }
    }
}
