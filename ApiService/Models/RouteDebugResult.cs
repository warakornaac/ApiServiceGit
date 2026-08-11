using System.Collections.Generic;

namespace ApiService.Models
{
    /// <summary>
    /// ผลลัพธ์แบบละเอียดของการ route ไปยัง SP ทั้ง 3 กลุ่ม ใช้สำหรับ debug ว่าแต่ละกลุ่ม
    /// ถูก trigger หรือไม่, ใช้ parameter อะไรยิงเข้า SP, และแต่ละ SP คืนอะไรกลับมาบ้าง
    /// (ก่อนจะ union+dedupe เป็น Items สุดท้ายที่ frontend เห็น)
    /// </summary>
    public class RouteDebugResult
    {
        public FieldRouteDebug Field { get; set; }
        public VioRouteDebug Vio { get; set; }
        public CategoryRouteDebug Category { get; set; }

        /// <summary>ผลรวมสุดท้ายหลัง union + dedupe ด้วย stkcode จากทั้ง 3 กลุ่ม (เหมือนที่ RouteAsync คืน)</summary>
        public List<ProductSearchVioDataResponse> MergedItems { get; set; }

        public RouteDebugResult() {
            MergedItems = new List<ProductSearchVioDataResponse>();
        }
    }

    /// <summary>Debug detail ของกลุ่ม description/oe/competitor -> GetProductBySearchField</summary>
    public class FieldRouteDebug
    {
        /// <summary>true = SearchType เข้าเกณฑ์กลุ่มนี้ และมีการยิงเข้า P_Search_Product_By_Field จริง</summary>
        public bool Triggered { get; set; }

        /// <summary>ค่า @inSearchText ที่ใช้ยิงจริง (token ที่ยาวที่สุดในกลุ่มนี้)</summary>
        public string SearchText { get; set; }

        /// <summary>ค่า @inSearchField (TVP) ที่ใช้ยิงจริง</summary>
        public List<string> SearchFields { get; set; }

        /// <summary>ผลลัพธ์ดิบที่ได้กลับมาจาก P_Search_Product_By_Field (ก่อน merge กับกลุ่มอื่น)</summary>
        public List<ProductSearchVioDataResponse> Items { get; set; }

        public FieldRouteDebug() {
            SearchFields = new List<string>();
            Items = new List<ProductSearchVioDataResponse>();
        }
    }

    /// <summary>Debug detail ของกลุ่ม model/maker -> GetProductBySearchVio</summary>
    public class VioRouteDebug
    {
        /// <summary>true = SearchType เข้าเกณฑ์กลุ่มนี้ และมีการยิงเข้า P_Search_Ktype_By_Car จริง</summary>
        public bool Triggered { get; set; }

        public string MakerId { get; set; }
        public string RangeId { get; set; }

        /// <summary>Ktype ทั้งหมดที่ได้จาก P_Search_Ktype_By_Car (ก่อนเอาไปหา product ต่อด้วย P_Search_Product_By_Ktype)</summary>
        public List<string> Ktypes { get; set; }

        /// <summary>ผลลัพธ์ดิบที่ได้กลับมาจาก P_Search_Product_By_Ktype (ก่อน merge กับกลุ่มอื่น)</summary>
        public List<ProductSearchVioDataResponse> Items { get; set; }

        public VioRouteDebug() {
            Ktypes = new List<string>();
            Items = new List<ProductSearchVioDataResponse>();
        }
    }

    /// <summary>Debug detail ของกลุ่ม productline/productgroup/brand -> GetProductBySearchCatagory</summary>
    public class CategoryRouteDebug
    {
        /// <summary>true = SearchType เข้าเกณฑ์กลุ่มนี้ และมีการยิงเข้า P_Search_Product_By_Catagory จริง</summary>
        public bool Triggered { get; set; }

        public List<string> ProductLineIds { get; set; }
        public List<string> ProductGroupIds { get; set; }
        public List<string> BrandIds { get; set; }

        /// <summary>ผลลัพธ์ดิบที่ได้กลับมาจาก P_Search_Product_By_Catagory (ก่อน merge กับกลุ่มอื่น)</summary>
        public List<ProductSearchVioDataResponse> Items { get; set; }

        public CategoryRouteDebug() {
            ProductLineIds = new List<string>();
            ProductGroupIds = new List<string>();
            BrandIds = new List<string>();
            Items = new List<ProductSearchVioDataResponse>();
        }
    }
}