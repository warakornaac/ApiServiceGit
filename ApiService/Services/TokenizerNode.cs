using System.Collections.Generic;
using ApiService.Models;

namespace ApiService.Services
{
    /// <summary>
    /// Node เดียวของ Trie ที่ใช้ในการตัดคำ
    /// แต่ละ node แทน 1 ตัวอักษร, ถ้า IsEndOfWord = true แปลว่ามีคำใน dictionary จบที่ node นี้พอดี
    /// </summary>
    public class TokenizerNode
    {
        public Dictionary<char, TokenizerNode> Children { get; private set; }

        public bool IsEndOfWord { get; set; }

        /// <summary>
        /// ข้อมูล dictionary ของคำที่จบที่ node นี้
        /// ใช้ List เพราะคำเดียวกันอาจมีได้หลาย SearchType เช่น "วีออส" เป็นได้ทั้ง Model และ Brand
        /// </summary>
        public List<SearchDictionaryModel> DictionaryEntries { get; private set; }

        public TokenizerNode()
        {
            Children = new Dictionary<char, TokenizerNode>();
            DictionaryEntries = new List<SearchDictionaryModel>();
        }

        public TokenizerNode GetOrAddChild(char c)
        {
            TokenizerNode child;
            if (!Children.TryGetValue(c, out child))
            {
                child = new TokenizerNode();
                Children[c] = child;
            }
            return child;
        }

        public bool TryGetChild(char c, out TokenizerNode child)
        {
            return Children.TryGetValue(c, out child);
        }
    }
}
