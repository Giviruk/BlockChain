using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using BlockChain.Common;
using BlockChain.Data;
using BlockChain.Entities;
using BlockChain.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BlockChain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace BlockChain.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration,
            ApplicationDbContext dbContext)
        {
            _logger = logger;
            _configuration = configuration;
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Index(VerifiedBlock verifiedBlock = null)
        {
            var blocks = _dbContext.Blocks.ToList().Select(x=>
            {
                var block = new BlockViewModel
                    {BlockNumber = x.BlockNumber, Data = x.Data, Hash = x.Hash, Signature = x.Signature};
                if (verifiedBlock?.VerifiedStatus==null || verifiedBlock.BlockNumber!=block.BlockNumber) 
                    block.Verified = "not tested";
                else
                {
                    block.Verified = verifiedBlock.VerifiedStatus;
                }

                return block;
            }).ToList();
            var privateKey = CryptoHelper.GetPrivateKeyString(_configuration[Consts.PrivateKeyPath.ToString()]);
            var publicKey = CryptoHelper.GetPublicKeyString(_configuration[Consts.PublicKeyPath.ToString()]);
            var model = new IndexModel()
            {
                Blocks = blocks,
                PrivateKey = privateKey,
                PublicKey = publicKey
            };
            return View(model);
        }

        [HttpGet]
        public IActionResult GenerateKeys()
        {
            CryptoHelper.GenerateKeys(_configuration[Consts.PrivateKeyPath.ToString()],
                _configuration[Consts.PublicKeyPath.ToString()]);
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }

        [HttpPost]
        public IActionResult CreateBlock([Required] string data)
        {
            var lastBlock = GetLastBlock();
            var privateKey = CryptoHelper.GetPrivateKey(_configuration[Consts.PrivateKeyPath.ToString()]);
            var signature = CryptoHelper.SignData(data, privateKey);
            string hash;
            Block newBlock;
            if (lastBlock == null)
            {
                hash = CryptoHelper.CreateHash(data);
                newBlock = new Block(0, data, hash, signature, DateTime.UtcNow);
            }
            else
            {
                hash = CryptoHelper.CreateHash(data, lastBlock);
                newBlock = new Block(lastBlock.BlockNumber + 1, data, hash, signature, DateTime.UtcNow);
            }

            _dbContext.Blocks.Add(newBlock);
            _dbContext.SaveChanges();
            return RedirectToAction("Index");
        }

        private Block GetLastBlock()
        {
            var blocks = _dbContext.Blocks;
            if (!blocks.Any()) return null;

            Block lastBlock = null;
            var lastBlockNumber = 0;
            foreach (var block in blocks)
            {
                if (block.BlockNumber < lastBlockNumber) continue;
                lastBlock = block;
                lastBlockNumber = block.BlockNumber;
            }

            return lastBlock;
        }

        [HttpGet]
        public IActionResult VerifyBlock(BlockViewModel block)
        {
            bool isVerified;
            var currentBlock = _dbContext.Blocks.Single(b => b.BlockNumber == block.BlockNumber);
            var verifiedBlock = new VerifiedBlock() {BlockNumber = block.BlockNumber};
            if (block.BlockNumber == 0)
            {
                isVerified =
                    CryptoHelper.VerifyBlock(currentBlock, _configuration[Consts.PublicKeyPath.ToString()]);
            }
            else
            {
                var previousBlock = _dbContext.Blocks.Single(b => b.BlockNumber == block.BlockNumber - 1);
                isVerified = CryptoHelper.VerifyBlock(currentBlock, previousBlock, _configuration[Consts.PublicKeyPath.ToString()]);
            }
            
            var verifiedStatus = isVerified ? "Verified" : "NOT VERIFIED";
            verifiedBlock.VerifiedStatus = verifiedStatus;
            return RedirectToAction("Index", verifiedBlock);
        }

        [HttpGet]
        public IActionResult VerifiedAll()
        {
            var blocks = _dbContext.Blocks.ToList();
            var verifiedChain = blocks.Select(x =>
            {
                var blockModel = new BlockViewModel()
                    {BlockNumber = x.BlockNumber, Data = x.Data, Hash = x.Hash, Signature = x.Signature};
                if (x.BlockNumber != 0)
                {
                    var previousBlock = _dbContext.Blocks.Single(b => b.BlockNumber == x.BlockNumber - 1);
                    var isVerified = CryptoHelper.VerifyBlock(x, previousBlock,
                        _configuration[Consts.PublicKeyPath.ToString()]);
                    blockModel.Verified = isVerified ? "Verified" : "NOT VERIFIED";
                }
                else
                {
                    var hash = CryptoHelper.CreateHash(x.Data);
                    blockModel.Verified = x.Hash == hash ? "Verified" : "NOT VERIFIED";
                }

                return blockModel;
            });
            using (var sw = new StreamWriter(_configuration[Consts.AllBlocksPath.ToString()], false, Encoding.Default))
            {
                sw.WriteLine(JsonSerializer.Serialize(verifiedChain));
            }

            return RedirectToAction("Index");
        }

    }
}