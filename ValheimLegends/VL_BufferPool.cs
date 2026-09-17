using System;
using System.Collections.Generic;

namespace ValheimLegends
{
    /// <summary>
    /// Pool de buffers seguro e reentrante para consultas de Character.GetCharactersInRange.
    /// Evita centenas de alocações de new List<Character>() no heap por frame/combate.
    /// Garante que todas as referências de entidades sejam limpas ao término de cada escopo (IDisposable),
    /// prevenindo retenção artificial de memória de GameObjects/Characters destruídos.
    /// </summary>
    public static class VL_BufferPool
    {
        private static readonly Stack<List<Character>> _pool = new Stack<List<Character>>(8);
        private static readonly object _lock = new object();

        public static List<Character> Acquire()
        {
            lock (_lock)
            {
                if (_pool.Count > 0)
                {
                    var list = _pool.Pop();
                    list.Clear();
                    return list;
                }
            }
            return new List<Character>(32);
        }

        public static void Release(List<Character> list)
        {
            if (list == null) return;
            list.Clear(); // Limpa referências para não manter Characters no heap

            lock (_lock)
            {
                if (_pool.Count < 16)
                {
                    _pool.Push(list);
                }
            }
        }

        public static CharacterListScope GetScope(out List<Character> buffer)
        {
            buffer = Acquire();
            return new CharacterListScope(buffer);
        }

        public readonly struct CharacterListScope : IDisposable
        {
            private readonly List<Character> _list;

            public CharacterListScope(List<Character> list)
            {
                _list = list;
            }

            public void Dispose()
            {
                if (_list != null)
                {
                    Release(_list);
                }
            }
        }
    }
}
