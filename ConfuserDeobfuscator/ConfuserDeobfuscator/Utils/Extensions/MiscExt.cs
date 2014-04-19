using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bea;
using ConfuserDeobfuscator.Engine;
using ConfuserDeobfuscator.Engine.Routines.Ex.x86;

namespace ConfuserDeobfuscator.Utils.Extensions
{
    public static class MiscExt
    {
        public static bool VerifyTop(this Stack<ILEmulator.StackEntry> stack)
        {
            var holder = stack.Peek();

            if (holder.IsValueKnown && stack.Peek().IsValueKnown)
            {
               // stack.Push(holder);
                return true;
            }

            stack.Push(holder);
            return false;
        }

        public static bool VerifyTop<T>(this Stack<ILEmulator.StackEntry> stack)
        {
            var holder = stack.Peek();

            if (stack.VerifyTop())
            {
                if (holder.Value.GetType().CanCastTo<T>(holder.Value) &&
                    stack.Peek().Value.GetType().CanCastTo<T>(stack.Peek().Value))
                {
                    stack.Push(holder);
                    return true;
                }
            }

            stack.Push(holder);
            return false;
        }

        public static uint GetUInt(this int val)
        {
            try
            {
                return Convert.ToUInt32(val);
            }
            catch (OverflowException)
            {
                return BitConverter.ToUInt32(BitConverter.GetBytes(Convert.ToInt32(val)), 0);
            }
        }

        public static ulong GetULong(this long val)
        {
            try
            {
                return Convert.ToUInt64(val);
            }
            catch (OverflowException)
            {
                return BitConverter.ToUInt64(BitConverter.GetBytes(Convert.ToInt64(val)), 0);
            }
        }

        public static bool TrueForAny<T>(IList<Predicate<T>> list, T param)
        {
            return list.Any(itm => itm(param));
        }

        public static void Foreach<T>(T[] arr, Action<T> action)
        {
            foreach (var itm in arr)
                action(itm);
        }

        //ugly
        public static bool IsNumeric(this object val)
        {
            return val is int || val is long || val is sbyte || val is short || val is ushort || val is ulong || val is uint || val is byte || val is double || val is decimal || val is float;
        }

        public static IX86Operand GetOperand(this ArgumentType argument)
        {
            if (argument.ArgType == -2013265920)
                return
                    new X86ImmediateOperand(int.Parse(argument.ArgMnemonic.TrimEnd('h'),
                        NumberStyles.HexNumber));
            return new X86RegisterOperand((X86Register)argument.ArgType);
        }

        public static Disasm Clone(this Disasm disasm)
        {
            return new Disasm
            {
                Archi = disasm.Archi,
                Argument1 = disasm.Argument1,
                Argument2 = disasm.Argument2,
                Argument3 = disasm.Argument3,
                CompleteInstr = disasm.CompleteInstr,
                EIP = disasm.EIP,
                Instruction = disasm.Instruction,
                Options = disasm.Options,
                Prefix = disasm.Prefix,
                SecurityBlock = disasm.SecurityBlock,
                VirtualAddr = disasm.VirtualAddr
            };
        }

        public static UnmanagedBuffer ToUnmanagedBuffer(this byte[] buff)
        {
            return new UnmanagedBuffer(buff);
        }

        public static bool CanCastTo<T>(this Type from, object val)
        {
            try
            {
                Convert.ChangeType(val, typeof(T));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
