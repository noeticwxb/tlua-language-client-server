﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace SharpLua
{
    using CharPtr = Lua.CharPtr;

    /// <summary>
    /// Lua scripts ran in LuaInterface as extension functions and clr libraries
    /// </summary>
    class ScriptStrings
    {
        // Precompiled data is used now because it is faster than parsing the source (twice!)
        // According to some basic tests, its 100 ms faster, which adds up...

#if false
        // Use precompiled chunks now

        #region LuaNet
        public const string InitLuaNet =
        @"local metatable = {}
        local rawget = rawget
        local import_type = luanet.import_type
        local load_assembly = luanet.load_assembly
        luanet.error, luanet.type = error, type
        -- Lookup a .NET identifier component.
        function metatable:__index(key) -- key is e.g. 'Form'
            -- Get the fully-qualified name, e.g. 'System.Windows.Forms.Form'
            local fqn = rawget(self,'.fqn')
            fqn = ((fqn and fqn .. '.') or '') .. key

            -- Try to find either a luanet function or a CLR type
            local obj = rawget(luanet,key) or import_type(fqn)

            -- If key is neither a luanet function or a CLR type, then it is simply
            -- an identifier component.
            if obj == nil then
                -- It might be an assembly, so we load it too.
                pcall(load_assembly,fqn)
                obj = { ['.fqn'] = fqn }
                setmetatable(obj, metatable)
            end

            -- Cache this lookup
            rawset(self, key, obj)
            return obj
        end

        -- A non-type has been called; e.g. foo = System.Foo()
        function metatable:__call(...)
            error('No such type: ' .. rawget(self,'.fqn'), 2)
        end

        -- This is the root of the .NET namespace
        luanet['.fqn'] = false
        setmetatable(luanet, metatable)

        -- Preload the mscorlib assembly
        luanet.load_assembly('mscorlib')";
        #endregion

        #region ClrLib
        public const string InitClrLib = @"
---
--- This lua module provides auto importing of .net classes into a named package.
--- Makes for super easy use of LuaInterface glue
---
--- example:
---   Threading = luanet.namespace('System', 'System.Threading')
---   Threading.Thread.Sleep(100)
---
--- Extensions:
--- import() is a version of luanet.namespace() which puts the package into a list which is used by a global __index lookup,
--- and thus works rather like C#'s using statement. It also recognizes the case where one is importing a local
--- assembly, which must end with an explicit .dll extension.

--- Alternatively, luanet.namespace can be used for convenience without polluting the global namespace:
---   local sys, sysio = luanet.namespace {'System','System.IO'}
--    sys.Console.WriteLine('we are at {0}',sysio.Directory.GetCurrentDirectory())

-- LuaInterface hosted with stock Lua interpreter will need to explicitly require this...
-- if not luanet then require 'luanet' end

local import_type, load_assembly = luanet.import_type, luanet.load_assembly

local mt = {
	--- Lookup a previously unfound class and add it to our table
	__index = function(package, classname)
		local class = rawget(package, classname)
		if class == nil then
			class = import_type(package.packageName .. '.' .. classname)
			package[classname] = class		-- keep what we found around, so it will be shared
		end
		return class
	end
}

function luanet.namespace(ns)
    if type(ns) == 'table' then
        local res = {}
        for i = 1, #ns do
            res[i] = luanet.namespace(ns[i])
        end
        return unpack(res)
    end
    -- FIXME - table.packageName could instead be a private index (see Lua 13.4.4)
    local t = { packageName = ns }
    setmetatable(t, mt)
    return t
end

local globalMT, packages

local function set_global_mt()
    packages = {}
    globalMT = {
        __index = function(T, classname)
                for i, package in ipairs(packages) do
                    local class = package[classname]
                    if class then
                        _G[classname] = class
                        return class
                    end
                end
        end
    }
    setmetatable(_G, globalMT)
end

function luanet.make_array(tp, tbl)
    local arr = tp[#tbl]
	for i, v in ipairs(tbl) do
	    arr:SetValue(v, i-1)
	end
	return arr
end

function luanet.each(o)
   local e = o:GetEnumerator()
   return function()
      if e:MoveNext() then
        return e.Current
     end
   end
end

-- Actual clr library
-- Provides methods for calling static methods, loading clr dll's, importing namespaces, 
-- and creating clr types.

local clr = { }

clr.ns = { ""System"", ""System.IO"", ""System.Windows.Forms"", ""System.Collections.Generic"", ""System.Text"" }

clr.load = luanet.load_assembly

clr.create = function(a, ...)
    local arg = { ... }
    local a2,b2 = pcall(function() return luanet.import_type(a) end)
--print(a2,b2)
    if a2 and b2 then return b2(...) end
    for k, ns in pairs(clr.ns) do
        local a3,b3 = pcall(function() return luanet.import_type(ns .. ""."" .. a) end)
--print(a3,b3)
        if a3 and b3 then return b3(...) end
    end
end

clr.call = function(f, ...)
    local type = """"
    local method = """"
    for i = 1, #f do
        if f:sub(i, i) == '.' then
            type = f:sub(1, i - 1)
            method = f:sub(i + 1)
        end
    end
    --print(type, method)
    local t = luanet.import_type(type)
    if t == nil then
        error('Unable to find type \'' .. type .. '\'')
    else
        local m = t[method]
        if m then 
            return m(...)
        else
            error([[No method ']] + method + [[' on type ']] + type '\'')
        end
    end
end

clr.usingns = function(ns)
    table.insert(clr.ns, ns)
end

clr.getns = luanet.namespace

clr.import = function(assemblyName, packageName)
    if not globalMT then
        set_global_mt()
    end
    if not packageName then
		local i = assemblyName:find('%.dll$')
		if i then packageName = assemblyName:sub(1, i-1)
		else packageName = assemblyName end
	end
    local t = luanet.namespace(assemblyName, packageName)
	table.insert(packages, t)
	return t
end

setmetatable(clr, { 
    __index = function(t, x)
        -- Return a System.* type
        return luanet.import_type('System.' .. x)
    end, 
    -- __tostring = ""clr library table"",
    })

_G.clr = clr
System = luanet.namespace(""System"")
";
        #endregion

        #region ExtLib
        public const string InitExtLib = @" -- Ext lib - a bunch of extension functions

function tobool(x)
    return x == true
end

-- _G.arg -> _G['...']
if arg then
    _G['...'] = { }
    for k, v in pairs(arg) do
        _G['...'][k] = v
    end
end

-- table.tolookup
function table.tolookup(t)
    local r = { }
    for k, v in pairs(t) do
        r[v] = true
    end
    return setmetatable(r, { __index = function() return false end })
end

-- override base type
rawtype = type
function type(o)
    local a, b = pcall(function()
        local mt = getmetatable(o)
        local t = rawtype(o)
        if rawtype(mt) == 'table' then   
            t = o.__type or mt.__type or mt._NAME or mt.ClassName
            if rawtype(t) == 'function' then t = t() end
        end 
        return t
    end)
    return a and b or rawtype(o)
end

-- table.invert
function table.invert(t)
    local r = { }
    for k, v in pairs(t) do
        r[v] = k
    end
    return r
end

function table.pack(...)
    local t = { ... }
    t.n = #t
    return t
end

function table.removeitem(t, i)
    t[i] = nil
end

function set(t, k, v)
    t[k] = v
end

local mt = getmetatable(table) or { }
-- create an empty table
mt.__call = function(t, ...) 
    local arg = { ... }
    arg[1] = arg[1] or 0
    arg[2] = arg[2] or 1
    arg[3] = arg[3] or 1

    local t = { }
    for i = arg[2], arg[1], arg[3] do
        if arg[i] then
            t[i] = arg[i]
        else
            if arg[4] then
                if type(arg[4]) == ""table"" then
                    t[i] = arg[4][i] or 0
                end
            else
                t[i] = 0
            end
        end
    end
    return t
end
setmetatable(table, mt)

Lua = { }
Lua.Parser = { }

Lua.Parser.Lex = function(code)
    local lexer = clr.create""SharpLua.Lexer""
    return lexer:Lex(code)
end

Lua.Parser.Parse = function(code)
    if type(code) == ""string"" then code = Lua.Parser.Lex(code) end
    local parser = clr.create(""SharpLua.Parser"", code)
    return parser:Parse()
end

Lua.CLR = clr
Lua.Clr = Lua.CLR
--Lua.String = System.String

SharpLua = luanet.namespace""SharpLua""

function math.round(x)
    return math.floor(x + 0.5)
end

function table.find(table, obj)
    for k, v in pairs(table) do
        if v == obj then
            return k
        end
    end
end

local strMt = debug.getmetatable(string) or { }
strMt.__index = function(t, k)
    if type(k) == ""number"" then
        return t:sub(k, k)
    end
    return string[k]
end
debug.setmetatable('', strMt)

function dostring(s)
    return loadstring(s)()
end

-- Courtesy of lua-users.org and metalua

function string.split(str, pat)
   local t = {} 
   local fpat = '(.-)' .. pat
   local last_end = 1
   local s, e, cap = string.find(str, fpat, 1)
   while s do
      if s ~= 1 or cap ~= '' then
          table.insert(t,cap)
       end
      last_end = e+1
      s, e, cap = string.find(str, fpat, last_end)
   end
   if last_end <= string.len(str) then
      cap = string.sub(str, last_end)
      table.insert(t, cap)
   end
   return t
end

string.strmatch = string.match

-- change a compiled string into a function
function string.undump(str)
   if str:strmatch '^\027LuaQ' or str:strmatch '^#![^\n]+\n\027LuaQ' then
      local f = (lua_loadstring or loadstring)(str)
      return f
   else
      error 'Not a chunk dump'
   end
end
function table.transpose(t)
   local tt = { }
   for a, b in pairs(t) do tt[b] = a end
   return tt
end

function table.iforeach(f, ...)
   -- assert (type (f) == 'function') [wouldn't allow metamethod __call]
   local nargs = select('#', ...)
   if nargs==1 then -- Quick iforeach (most common case), just one table arg
      local t = ...
      assert (type (t) == 'table')
      for i = 1, #t do 
         local result = f (t[i])
         -- If the function returns non-false, stop iteration
         if result then return result end
      end
   else -- advanced case: boundaries and/or multiple tables

      -- fargs:       arguments fot a single call to f
      -- first, last: indexes of the first & last elements mapped in each table
      -- arg1:        index of the first table in args

      -- 1 - find boundaries if any
      local  args, fargs, first, last, arg1 = {...}, { }
      if     type(args[1]) ~= 'number' then first, arg1 = 1, 1 -- no boundary
      elseif type(args[2]) ~= 'number' then first, last, arg1 = 1, args[1], 2
      else   first,  last, arg1 = args[1], args[2], 3 end
      assert (nargs >= arg1) -- at least one table
      -- 2 - determine upper boundary if not given
      if not last then for i = arg1, nargs do 
            assert (type (args[i]) == 'table')
            last = max (#args[i], last) 
      end end
      -- 3 - remove non-table arguments from args, adjust nargs
      if arg1>1 then args = { select(arg1, unpack(args)) }; nargs = #args end

      -- 4 - perform the iteration
      for i = first, last do
         for j = 1, nargs do fargs[j] = args[j][i] end -- build args list
         local result = f (unpack (fargs)) -- here is the call
         -- If the function returns non-false, stop iteration
         if result then return result end
      end
   end
end

function table.imap (f, ...)
   local result, idx = { }, 1
   local function g(...) result[idx] = f(...);  idx=idx+1 end
   table.iforeach(g, ...)
   return result
end

function table.ifold (f, acc, ...)
   local function g(...) acc = f (acc,...) end
   table.iforeach (g, ...)
   return acc
end

-- function table.ifold1 (f, ...)
--    return table.ifold (f, acc, 2, false, ...)
-- end

function table.izip(...)
   local function g(...) return {...} end
   return table.imap(g, ...)
end

function table.ifilter(f, t)
   local yes, no = { }, { }
   for i=1,#t do table.insert (f(t[i]) and yes or no, t[i]) end
   return yes, no
end

function table.icat(...)
   local result = { }
   for t in values {...} do
      for x in values (t) do
         table.insert (result, x)
      end
   end
   return result
end

function table.iflatten (x) return table.icat (unpack (x)) end

function table.irev (t)
   local result, nt = { }, #t
   for i=0, nt-1 do result[nt-i] = t[i+1] end
   return result
end

function table.isub (t, ...)
   local ti, u = table.insert, { }
   local args, nargs = {...}, select('#', ...)
   for i=1, nargs/2 do
      local a, b = args[2*i-1], args[2*i]
      for i=a, b, a<=b and 1 or -1 do ti(u, t[i]) end
   end
   return u
end

function table.iall (f, ...)
   local result = true
   local function g(...) return not f(...) end
   return not table.iforeach(g, ...)
   --return result
end

function table.iany (f, ...)
   local function g(...) return not f(...) end
   return not table.iall(g, ...)
end

function table.shallow_copy(x)
   local y={ }
   for k, v in pairs(x) do y[k]=v end
   return y
end

-- Warning, this is implementation dependent: it relies on
-- the fact the [next()] enumerates the array-part before the hash-part.
function table.cat(...)
   local y={ }
   for x in values{...} do
      -- cat array-part
      for _, v in ipairs(x) do table.insert(y,v) end
      -- cat hash-part
      local lx, k = #x
      if lx>0 then k=next(x,lx) else k=next(x) end
      while k do y[k]=x[k]; k=next(x,k) end
   end
   return y
end

function table.deep_copy(x) 
   local tracker = { }
   local function aux (x)
      if type(x) == 'table' then
         local y=tracker[x]
         if y then return y end
         y = { }; tracker[x] = y
         setmetatable (y, getmetatable (x))
         for k,v in pairs(x) do y[aux(k)] = aux(v) end
         return y
      else return x end
   end
   return aux(x)
end

function table.override(dst, src)
   for k, v in pairs(src) do dst[k] = v end
   for i = #src+1, #dst   do dst[i] = nil end
   return dst
end


function table.range(a,b,c)
   if not b then assert(not(c)); b=a; a=1
   elseif not c then c = (b>=a) and 1 or -1 end
   local result = { }
   for i=a, b, c do table.insert(result, i) end
   return result
end

-- FIXME: new_indent seems to be always nil?!
-- FIXME: accumulator function should be configurable,
-- so that print() doesn't need to bufferize the whole string
-- before starting to print.
function table.tostring(t, ...)
   local PRINT_HASH, HANDLE_TAG, FIX_INDENT, LINE_MAX, INITIAL_INDENT = true, true
   for _, x in ipairs {...} do
      if type(x) == 'number' then
         if not LINE_MAX then LINE_MAX = x
         else INITIAL_INDENT = x end
      elseif x=='nohash' then PRINT_HASH = false
      elseif x=='notag'  then HANDLE_TAG = false
      else
         local n = string['match'](x, '^indent%s*(%d*)$')
         if n then FIX_INDENT = tonumber(n) or 3 end
      end
   end
   LINE_MAX       = LINE_MAX or math.huge
   INITIAL_INDENT = INITIAL_INDENT or 1
   
   local current_offset =  0  -- indentation level
   local xlen_cache     = { } -- cached results for xlen()
   local acc_list       = { } -- Generated bits of string
   local function acc(...)    -- Accumulate a bit of string
      local x = table.concat{...}
      current_offset = current_offset + #x
      table.insert(acc_list, x) 
   end
   local function valid_id(x)
      -- FIXME: we should also reject keywords; but the list of
      -- current keywords is not fixed in metalua...
      return type(x) == 'string' 
         and string['match'](x, '^[a-zA-Z_][a-zA-Z0-9_]*$')
   end
   
   -- Compute the number of chars it would require to display the table
   -- on a single line. Helps to decide whether some carriage returns are
   -- required. Since the size of each sub-table is required many times,
   -- it's cached in [xlen_cache].
   local xlen_type = { }
   local function xlen(x, nested)
      nested = nested or { }
      if x==nil then return #'nil' end
      --if nested[x] then return #tostring(x) end -- already done in table
      local len = xlen_cache[x]
      if len then return len end
      local f = xlen_type[type(x)]
      if not f then return #tostring(x) end
      len = f (x, nested) 
      xlen_cache[x] = len
      return len
   end

   -- optim: no need to compute lengths if I'm not going to use them
   -- anyway.
   if LINE_MAX == math.huge then xlen = function() return 0 end end

   xlen_type['nil'] = function () return 3 end
   function xlen_type.number  (x) return #tostring(x) end
   function xlen_type.boolean (x) return x and 4 or 5 end
   function xlen_type.string  (x) return #string.format('%q',x) end
   function xlen_type.table   (adt, nested)

      -- Circular references detection
      if nested [adt] then return #tostring(adt) end
      nested [adt] = true

      local has_tag  = HANDLE_TAG and valid_id(adt.tag)
      local alen     = #adt
      local has_arr  = alen>0
      local has_hash = false
      local x = 0
      
      if PRINT_HASH then
         -- first pass: count hash-part
         for k, v in pairs(adt) do
            if k=='tag' and has_tag then 
               -- this is the tag -> do nothing!
            elseif type(k)=='number' and k<=alen and math.fmod(k,1)==0 then 
               -- array-part pair -> do nothing!
            else
               has_hash = true
               if valid_id(k) then x=x+#k
               else x = x + xlen (k, nested) + 2 end -- count surrounding brackets
               x = x + xlen (v, nested) + 5          -- count ' = ' and ', '
            end
         end
      end

      for i = 1, alen do x = x + xlen (adt[i], nested) + 2 end -- count ', '
      
      nested[adt] = false -- No more nested calls

      if not (has_tag or has_arr or has_hash) then return 3 end
      if has_tag then x=x+#adt.tag+1 end
      if not (has_arr or has_hash) then return x end
      if not has_hash and alen==1 and type(adt[1])~='table' then
         return x-2 -- substract extraneous ', '
      end
      return x+2 -- count '{ ' and ' }', substract extraneous ', '
   end
   
   -- Recursively print a (sub) table at given indentation level.
   -- [newline] indicates whether newlines should be inserted.
   local function rec (adt, nested, indent)
      if not FIX_INDENT then indent = current_offset end
      local function acc_newline()
         acc ('\n\''); acc (string.rep (' ', indent)) 
         current_offset = indent
      end
      local x = { }
      x['nil'] = function() acc 'nil' end
      function x.number()   acc (tostring (adt)) end
      --function x.string()   acc (string.format ('%q', adt)) end
      function x.string()   acc ((string.format ('%q', adt):gsub('\\\n', '\\n'))) end
      function x.boolean()  acc (adt and 'true' or 'false') end
      function x.table()
         if nested[adt] then acc(tostring(adt)); return end
         nested[adt]  = true


         local has_tag  = HANDLE_TAG and valid_id(adt.tag)
         local alen     = #adt
         local has_arr  = alen>0
         local has_hash = false

         if has_tag then acc('`'); acc(adt.tag) end

         -- First pass: handle hash-part
         if PRINT_HASH then
            for k, v in pairs(adt) do
               -- pass if the key belongs to the array-part or is the 'tag' field
               if not (k=='tag' and HANDLE_TAG) and 
                  not (type(k)=='number' and k<=alen and math.fmod(k,1)==0) then

                  -- Is it the first time we parse a hash pair?
                  if not has_hash then 
                     acc '{ '
                     if not FIX_INDENT then indent = current_offset end
                  else acc ', ' end

                  -- Determine whether a newline is required
                  local is_id, expected_len = valid_id(k)
                  if is_id then expected_len = #k + xlen (v, nested) + #' = , '
                  else expected_len = xlen (k, nested) + 
                                      xlen (v, nested) + #'[] = , ' end
                  if has_hash and expected_len + current_offset > LINE_MAX
                  then acc_newline() end
                  
                  -- Print the key
                  if is_id then acc(k); acc ' = ' 
                  else  acc '['; rec (k, nested, indent+(FIX_INDENT or 0)); acc '] = ' end

                  -- Print the value
                  rec (v, nested, indent+(FIX_INDENT or 0))
                  has_hash = true
               end
            end
         end

         -- Now we know whether there's a hash-part, an array-part, and a tag.
         -- Tag and hash-part are already printed if they're present.
         if not has_tag and not has_hash and not has_arr then acc '{ }'; 
         elseif has_tag and not has_hash and not has_arr then -- nothing, tag already in acc
         else 
            assert (has_hash or has_arr)
            local no_brace = false
            if has_hash and has_arr then acc ', ' 
            elseif has_tag and not has_hash and alen==1 and type(adt[1])~='table' then
               -- No brace required; don't print '{', remember not to print '}'
               acc (' '); rec (adt[1], nested, indent+(FIX_INDENT or 0))
               no_brace = true
            elseif not has_hash then
               -- Braces required, but not opened by hash-part handler yet
               acc '{ '
               if not FIX_INDENT then indent = current_offset end
            end

            -- 2nd pass: array-part
            if not no_brace and has_arr then 
               rec (adt[1], nested, indent+(FIX_INDENT or 0))
               for i=2, alen do 
                  acc ', ';                   
                  if   current_offset + xlen (adt[i], { }) > LINE_MAX
                  then acc_newline() end
                  rec (adt[i], nested, indent+(FIX_INDENT or 0)) 
               end
            end
            if not no_brace then acc ' }' end
         end
         nested[adt] = false -- No more nested calls
      end
      local y = x[type(adt)]
      if y then y() else acc(tostring(adt)) end
   end
   --printf('INITIAL_INDENT = %i', INITIAL_INDENT)
   current_offset = INITIAL_INDENT or 0
   rec(t, { }, 0)
   return table.concat (acc_list)
end

function table.print(...) return print(table.tostring(...)) end
";
        #endregion
#else
        static string luanet = "";
        public static string InitLuaNet
        {
            get
            {
                if (luanet != "") // cache for faster lookup times
                    return luanet;

                string s2 = "";
                Stream s = GetEmbeddedResource("SharpLua.Resources.luanet.sluac");

                byte[] buffer = new byte[(int)s.Length];
                s.Read(buffer, 0, (int)s.Length);

                foreach (byte b in buffer)
                    s2 += (char)b;

                s.Close();
                luanet = s2;
                return luanet;
            }
        }

        static string clrlib = "";
        public static string InitClrLib
        {
            get
            {
                if (clrlib != "") // cache for faster lookup times
                    return clrlib;

                string s2 = "";
                Stream s = GetEmbeddedResource("SharpLua.Resources.clrlib.sluac");

                byte[] buffer = new byte[(int)s.Length];
                s.Read(buffer, 0, (int)s.Length);

                foreach (byte b in buffer)
                    s2 += (char)b;

                s.Close();
                clrlib = s2;
                return clrlib;
            }
        }

        static string extlib = "";
        public static string InitExtLib
        {
            get
            {
                if (extlib != "") // cache for faster lookup times
                    return extlib;

                string s2 = "";
                Stream s = GetEmbeddedResource("SharpLua.Resources.extlib.sluac");

                byte[] buffer = new byte[(int)s.Length];
                s.Read(buffer, 0, (int)s.Length);

                foreach (byte b in buffer)
                    s2 += (char)b;

                s.Close();
                extlib = s2;
                return extlib;
            }
        }

        static Assembly asm = null;
        /// <summary>
        /// Assembly defaults to the current IExtendFramework assembly
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="asm"></param>
        /// <returns></returns>
        public static Stream GetEmbeddedResource(string resourceName)
        {
            if (asm == null)
                asm = Assembly.GetExecutingAssembly();
            //Assembly a1 = Assembly.GetExecutingAssembly();
            //Stream s = a1.GetManifestResourceStream(resourceName);
            //return s;
            Stream s = asm.GetManifestResourceStream(resourceName);
            if (s == null)
                throw new Exception("Could not load '" + resourceName + "' from '" + asm.ToString() + "'!");

            return s;
        }
#endif
    }
}
