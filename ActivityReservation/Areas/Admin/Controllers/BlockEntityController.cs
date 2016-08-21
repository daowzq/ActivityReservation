﻿using ActivityReservation.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using Business;

namespace ActivityReservation.Areas.Admin.Controllers
{
    /// <summary>
    /// 黑名单
    /// </summary>
    [Authorize]
    [Filters.PermissionRequired]
    public class BlockEntityController : Controller
    {
        /// <summary>
        /// logger
        /// </summary>
        private static Common.LogHelper logger = new Common.LogHelper(typeof(BlockEntityController));

        private static Business.BLLBlockEntity handler = null;        
        private static BLLBlockEntity Handler
        {
            get
            {
                if (handler == null)
                {
                    handler = new BLLBlockEntity();
                }
                return handler;
            }            
        }

        // GET: Admin/BlockEntity
        public ActionResult Index()
        {
            List<Models.BlockType> types = new Business.BLLBlockType().GetAll();
            return View(types);
        }

        /// <summary>
        /// 数据视图
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public ActionResult List(SearchHelperModel search)
        {
            //默认查询全部
            Expression<Func<Models.BlockEntity, bool>> whereLambda = (b => 1==1);
            //判断查询条件
            if (!String.IsNullOrEmpty(search.SearchItem1) && !("0".Equals(search.SearchItem1)))
            {
                Guid id = Guid.Parse(search.SearchItem1);
                if (!String.IsNullOrEmpty(search.SearchItem2))
                {
                    whereLambda = (b => b.BlockTypeId == id && b.BlockValue.Contains(search.SearchItem2));
                }
                else
                {
                    whereLambda = (b => b.BlockTypeId == id);                    
                }
            }
            else
            {                
                if (!String.IsNullOrEmpty(search.SearchItem2))
                {
                    whereLambda = (b => b.BlockValue.Contains(search.SearchItem2));
                }
            }
            int rowsCount = 0;
            try
            {
                List<Models.BlockEntity> blockList = new Business.BLLBlockEntity().GetPagedList(search.PageIndex, search.PageSize, out rowsCount, whereLambda, b => b.BlockTime, false);
                PagerModel pager = new PagerModel(search.PageIndex, search.PageSize, rowsCount);
                PagedListModel<Models.BlockEntity> dataList = new PagedListModel<Models.BlockEntity>() { Pager = pager, Data = blockList };
                return View(dataList);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// 添加到黑名单
        /// </summary>
        /// <param name="model">黑名单model</param>
        /// <returns></returns>
        public ActionResult AddEntity(Guid typeId,string blockValue)
        {
            if (ModelState.IsValid)
            {
                Models.BlockEntity entity = new Models.BlockEntity()
                {
                    BlockId = Guid.NewGuid(),
                    BlockTypeId = typeId,
                    BlockValue = blockValue,
                    BlockTime = DateTime.Now
                };
                try
                {
                    int count = Handler.Add(entity);
                    if (count == 1)
                    {
                        //记录日志
                        OperLogHelper.AddOperLog(String.Format("添加 {0} 到黑名单", blockValue), Module.BlockEntity, (Session["User"] as Models.User).UserName);
                        return Json(true);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
                return Json(false);
            }
            else
            {
                return Json(false);
            }
        }

        /// <summary>
        /// 更新黑名单实体状态
        /// </summary>
        /// <param name="entityId">黑名单数据id</param>
        /// <param name="status">状态</param>
        /// <returns></returns>
        public ActionResult UpdateEntityStatus(Guid entityId, string entityName, int status)
        {
            Models.BlockEntity entity = new Models.BlockEntity() { BlockId = entityId };
            if (status > 0)
            {
                entity.IsActive = true;
            }
            else
            {
                entity.IsActive = false;
            }
            try
            {
                int count = Handler.Update(entity,"IsActive");
                if (count>0)
                {
                    OperLogHelper.AddOperLog(String.Format("更改黑名单 {0} 状态为 {1}", entityName, entity.IsActive?"启用":"禁用"), Module.BlockEntity, (Session["User"] as Models.User).UserName);
                    return Json(true);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return Json(false);
        }

        /// <summary>
        /// 从黑名单中删除一条数据
        /// </summary>
        /// <param name="entityId">黑名单数据id</param>
        /// <param name="entityName">黑名单数据名称</param>
        /// <returns></returns>
        public ActionResult DeleteEntity(Guid entityId,string entityName)
        {
            try
            {
                int c = Handler.Delete(new Models.BlockEntity() { BlockId = entityId });
                if (c == 1)
                {
                    //记录日志
                    OperLogHelper.AddOperLog(String.Format("删除黑名单 {0}", entityName), Module.BlockEntity, (Session["User"] as Models.User).UserName);
                    return Json(true);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            return Json(false);
        }
    }
}