﻿using System;
using Sp.Settle.Constants;

namespace Sp.Settle.Models
{
    public class RefundRequest
    {
        /// <summary>
        ///     支付提供商在支付时返回的id
        /// </summary>
        public string ProviderId { get; set; }

        /// <summary>
        ///     支付后台系统生成的退款流水号
        /// </summary>
        public string RefundId { get; set; }

        /// <summary>
        ///     支付通道
        /// </summary>
        public Channel Channel { get; set; }

        /// <summary>
        ///     支付订单号
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        ///     订单支付金额
        /// </summary>
        public int PayAmount { get; set; }

        /// <summary>
        ///     本次退款金额
        /// </summary>
        public int RefundAmount { get; set; }

        /// <summary>
        ///     退款原因
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     支付时间
        /// </summary>
        public DateTime PayTime { get; set; }
    }
}