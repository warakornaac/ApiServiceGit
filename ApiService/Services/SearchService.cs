using ApiService.Models;
using ApiService.Repository;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiService.Services
{
    public class SearchService
    {
        private readonly NormalizeService _normalize =
            new NormalizeService();

        private readonly AutomotiveTokenizer _tokenizer =
            new AutomotiveTokenizer();

        private readonly MeiliSearchClient _meili =
            new MeiliSearchClient();

        private readonly SearchParser _parser =
            new SearchParser();

        private readonly SqlRepository _repository =
            new SqlRepository();

        public async Task<GlobalSearchResult> GlobalSearch(string keyword) {
            //----------------------------------------
            // STEP 1 Normalize
            //----------------------------------------

            keyword = NormalizeService.Normalize(keyword);

            //----------------------------------------
            // STEP 2 Tokenize
            //----------------------------------------

            List<string> tokens =
                await _tokenizer.Tokenize(keyword);

            if (tokens.Count == 0)
                tokens.Add(keyword);

            //----------------------------------------
            // STEP 3 Search Dictionary
            //----------------------------------------

            List<SearchDictionaryModel> dictionary =
                await _meili.SearchBatch(tokens);

            //----------------------------------------
            // STEP 4 Ranking
            //----------------------------------------

            dictionary =
                dictionary
                    .OrderByDescending(x => x.Priority)
                    .ThenByDescending(x => x._rankingScore)
                    .ToList();

            //----------------------------------------
            // STEP 5 Build SQL Request
            //----------------------------------------

            SearchSqlRequest request =
                _parser.BuildRequest(
                    keyword,
                    dictionary);

            //----------------------------------------
            // STEP 6 SQL
            //----------------------------------------

            //List<ProductSearchVioDataResponse> products =
            //    _repository.Search(request);
            List<ProductSearchVioDataResponse> products =
    new List<ProductSearchVioDataResponse>();

            products.Add(new ProductSearchVioDataResponse {
                stkcode = request.Keyword,
                stkcodeDescription = string.Join(", ", request.SearchTypes)
            });

            //----------------------------------------
            // STEP 7 Response
            //----------------------------------------

            //return new GlobalSearchResult {
            //    Success = true,

            //    Message = "Search Success",

            //    NormalizeKeyword =
            //        request.Keyword,

            //    SearchTypes =
            //        request.SearchTypes,

            //    MeiliHits =
            //        dictionary,

            //    Items =
            //        products
            //};
            //return new GlobalSearchResult {
            //    Success = true,
            //    Message = "Search from Meilisearch",
            //    NormalizeKeyword = request.Keyword,
            //    SearchTypes = request.SearchTypes,
            //    MeiliHits = dictionary,
            //    Items = products
            //};
            return new GlobalSearchResult {
                Success = true,

                Message = "Search from Meilisearch",

                NormalizeKeyword = request.Keyword,

                Tokens = tokens,

                SearchTypes = request.SearchTypes,

                SqlRequest = request,

                MeiliHits = dictionary,

                Items = products
            };
        }
    }
}