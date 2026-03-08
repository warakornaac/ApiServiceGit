using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiService.Models
{
    public class SmsModels
    {
        public string Phone { get; set; }
        public string Text { get; set; }
        public string User { get; set; }
    }
    public class SendDeliveryModels
    {
        public string Uid { get; set; }
        public string Docno { get; set; }
        public string Docdate { get; set; }
        public string Cusname { get; set; }
        public string Delivery { get; set; }
        public string DeliveryId { get; set; }
        public string Urldetail { get; set; }
        public string User { get; set; }

    }
    public class SendPmApproved
    {
        public string Uid { get; set; }
        public string Topic { get; set; }
        public string Topiccod { get; set; }
        public string Stkcod { get; set; }
        public string Stkdes { get; set; }
        public string SPrice { get; set; }
        public string Qty { get; set; }
        public string Cuscod { get; set; }
        public string Cusname { get; set; }
        public string ApprvDate { get; set; }
        public string ApprvBy { get; set; }
        // public string Urllink { get; set; }
        public string User { get; set; }
        public string Sta { get; set; }

    }
    public class SmsResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
    }
    public class SmsApiResponse
    {
        public int RemainingCredit { get; set; }
        public int TotalUseCredit { get; set; }
        public string CreditType { get; set; }
        public PhoneNumberListItem[] PhoneNumberList { get; set; }
        public object[] BadPhoneNumberList { get; set; }
    }
    public class PhoneNumberListItem
    {
        public string Number { get; set; }
        public string MessageId { get; set; }
        public int UsedCredit { get; set; }
    }



    //API CHATBOT
    public class ApiResponse<T>
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
    public class NVBackOrder
    {
        public string order_number { get; set; }
        public DateTime recorded_date { get; set; }
        public string customer_code { get; set; }
        public string customer_name { get; set; }
        public string part_no { get; set; }
        public string product_name { get; set; }
        public int bo_quantity { get; set; }
        public int stock_available_for_sale { get; set; }
        public decimal amount { get; set; }

    }
    public class StkPrice
    {
        public string customer_code { get; set; }
        public string part_no { get; set; }
        public string product_name { get; set; }
        public string brand { get; set; }
        public string company { get; set; }
        public decimal structure_price { get; set; }
        public decimal special_price { get; set; }
        public decimal previous_price { get; set; }
        public int stock_quantity { get; set; }
        public DateTime? estimated_arrival_date { get; set; }
        public int moq { get; set; }
        public int sales_packing_standard { get; set; }
        public bool is_eop { get; set; }
        public string subtitute_product { get; set; }

    }
    public class StkPrice_false
    {
        public string customer_code { get; set; }
        public string part_no { get; set; }
        public string product_name { get; set; }
        public string brand { get; set; }
        public string company { get; set; }
        public decimal structure_price { get; set; }
        public decimal special_price { get; set; }
        public decimal previous_price { get; set; }
        public DateTime? estimated_arrival_date { get; set; }
        public int moq { get; set; }
        public int sales_packing_standard { get; set; }
        public bool is_eop { get; set; }
        public string subtitute_product { get; set; }
    }
    public class StkDeliveryHead<T>
    {
        public string order_number { get; set; }
        public DateTime purchase_date { get; set; }
        public string customer_name { get; set; }
        public string customer_code { get; set; }
        public T product { get; set; }
        public string deivery_status { get; set; }
        public DateTime? esimated_arrival_date { get; set; }
    }
    public class product_detail
    {
        [JsonIgnore]
        public string order_number { get; set; }

        public string part_no { get; set; }
        public string product_name { get; set; }
        public int order_quantity { get; set; }
        public decimal total { get; set; }

    }
    public class Customer
    {
        public string customer_code { get; set; }
        public string customer_name { get; set; }
    }

    public class StkPrc
    {
        public string customer_code { get; set; }
        public string customer_name { get; set; }
    }
}