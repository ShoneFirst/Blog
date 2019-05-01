﻿using MeowvBlog.Core.Configuration;
using MeowvBlog.Core.Domain;
using MeowvBlog.Core.Domain.Articles;
using MeowvBlog.Core.Domain.Articles.Repositories;
using MeowvBlog.Core.Domain.Categories.Repositories;
using MeowvBlog.Core.Domain.Tags.Repositories;
using MeowvBlog.Services.Dto.Articles;
using MeowvBlog.Services.Dto.Articles.Params;
using MeowvBlog.Services.Dto.Categories;
using MeowvBlog.Services.Dto.Common;
using MeowvBlog.Services.Dto.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UPrime;
using UPrime.AutoMapper;
using UPrime.Services.Dto;

namespace MeowvBlog.Services.Articles.Impl
{
    /// <summary>
    /// 文章服务接口实现
    /// </summary>
    public partial class ArticleService : ServiceBase, IArticleService
    {
        private readonly IArticleRepository _articleRepository;
        private readonly IArticleCategoryRepository _articleCategoryRepository;
        private readonly IArticleTagRepository _articleTagRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITagRepository _tagRepository;

        public ArticleService(IArticleRepository articleRepository, IArticleCategoryRepository articleCategoryRepository, IArticleTagRepository articleTagRepository, ICategoryRepository categoryRepository, ITagRepository tagRepository)
        {
            _articleRepository = articleRepository;
            _articleCategoryRepository = articleCategoryRepository;
            _articleTagRepository = articleTagRepository;

            _categoryRepository = categoryRepository;
            _tagRepository = tagRepository;
        }

        /// <summary>
        /// 当前数据库是否为SqlServer
        /// </summary>
        public static bool IsSqlServer => AppSettings.DbType == GlobalConsts.DBTYPE_SQLSERVER;

        /// <summary>
        /// 获取一篇文章详细信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionOutput<ArticleDto>> GetAsync(int id)
        {
            var output = new ActionOutput<ArticleDto>();

            if (id <= 0)
            {
                output.AddError(GlobalConsts.PARAMETER_ERROR);
                return output;
            }

            using (var uow = UnitOfWorkManager.Begin())
            {
                var entity = await _articleRepository.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false);
                if (entity.IsNull())
                {
                    output.AddError(GlobalConsts.NONE_DATA);
                    return output;
                }

                output.Result = entity.MapTo<ArticleDto>();

                await uow.CompleteAsync();
            }

            return output;
        }

        /// <summary>
        /// 分页获取文章列表
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<PagedResultDto<GetArticleListOutput>> GetListAsync(PagingInput input)
        {
            using (var uow = UnitOfWorkManager.Begin())
            {
                var query = await _articleRepository.GetAllListAsync(x => x.IsDeleted == false);

                var count = query.Count;

                var list = new List<GetArticleListOutput>();

                var articles = query.OrderByDescending(x => x.PostTime)
                                    .ThenByDescending(x => x.Id)
                                    .AsQueryable()
                                    .PageByIndex(input.PageIndex, input.PageSize)
                                    .ToList();
                foreach (var item in articles)
                {
                    var catgegoryId = _articleCategoryRepository.FirstOrDefaultAsync(x => x.ArticleId == item.Id).Result.CategoryId;
                    var category = await _categoryRepository.FirstOrDefaultAsync(x => x.Id == catgegoryId);

                    var tagIds = _articleTagRepository.GetAllListAsync(x => x.ArticleId == item.Id).Result.Select(x => x.TagId);
                    var tags = await _tagRepository.GetAllListAsync(x => tagIds.Contains(x.Id));

                    var output = new GetArticleListOutput
                    {
                        Article = new ArticleBriefDto()
                        {
                            Id = item.Id,
                            Title = item.Title,
                            Author = item.Author,
                            Source = item.Source,
                            Url = item.Url,
                            Summary = item.Summary,
                            PostTime = item.PostTime
                        },
                        Category = category.MapTo<CategoryDto>(),
                        Tags = tags.Take(3).MapTo<IList<TagDto>>()
                    };
                    list.Add(output);
                }

                await uow.CompleteAsync();

                return new PagedResultDto<GetArticleListOutput>(count, list);
            }
        }

        /// <summary>
        /// 新增文章
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<ActionOutput<string>> InsertAsync(InsertArticleInput input)
        {
            var output = new ActionOutput<string>();

            using (var uow = UnitOfWorkManager.Begin())
            {
                var entity = new Article
                {
                    Title = input.Title,
                    Author = input.Author,
                    Source = input.Source,
                    Url = input.Url,
                    Summary = input.Summary,
                    Content = input.Content,
                    Hits = 0,
                    CreationTime = DateTime.Now,
                    PostTime = input.PostTime,
                    IsDeleted = false
                };
                await _articleRepository.InsertAsync(entity);

                output.Result = GlobalConsts.INSERT_SUCCESS;

                await uow.CompleteAsync();
            }
            return output;
        }

        /// <summary>
        /// 更新文章
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<ActionOutput<string>> UpdateAsync(UpdateArticleInput input)
        {
            var output = new ActionOutput<string>();

            using (var uow = UnitOfWorkManager.Begin())
            {
                var entity = await _articleRepository.GetAsync(input.ArticleId);
                entity.Title = input.Title;
                entity.Author = input.Author;
                entity.Source = input.Source;
                entity.Url = input.Url;
                entity.Summary = input.Summary;
                entity.Content = input.Content;
                entity.PostTime = input.PostTime;
                await _articleRepository.UpdateAsync(entity);

                output.Result = GlobalConsts.UPDATE_SUCCESS;

                await uow.CompleteAsync();
            }
            return output;
        }

        /// <summary>
        /// 删除文章
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<ActionOutput<string>> DeleteAsync(DeleteInput input)
        {
            var output = new ActionOutput<string>();

            using (var uow = UnitOfWorkManager.Begin())
            {
                var entity = await _articleRepository.GetAsync(input.Id);
                entity.IsDeleted = true;
                await _articleRepository.UpdateAsync(entity);

                output.Result = GlobalConsts.DELETE_SUCCESS;

                await uow.CompleteAsync();
            }
            return output;
        }
    }
}