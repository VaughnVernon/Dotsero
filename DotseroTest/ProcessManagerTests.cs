namespace DotseroTest
{
    using Dotsero.Actor;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    [TestClass]
    public class ProcessManagerTests
    {
        [TestMethod]
        public void LoanBrokerBestLoanQuotation()
        {
            AutoResetEvent termination = new AutoResetEvent(false);

            var system = ActorSystem.Create("ProcessManagerTests");

            var creditBureau = system.ActorOf(typeof(CreditBureau), Props.None, "creditBureau");
  
            var bank1 = system.ActorOf(typeof(Bank), Props.With("bank1", 2.75, 0.30), "bank1");
            var bank2 = system.ActorOf(typeof(Bank), Props.With("bank2", 2.73, 0.31), "bank2");
            var bank3 = system.ActorOf(typeof(Bank), Props.With("bank3", 2.80, 0.29), "bank3");
  
            var loanBroker =
                system.ActorOf(
                    typeof(LoanBroker),
                Props.With(creditBureau, new ActorRef[] { bank1, bank2, bank3 }, termination), "loanBroker");

            loanBroker.Tell(new QuoteBestLoanRate("111-11-1111", 100000, 84));

            var set1 = termination.WaitOne(2000, false);

            Assert.IsTrue(set1);
        }
    }

    /////////////////////////////////////////////////////////
    /////////////////////// ProcessManager
    /////////////////////////////////////////////////////////

    public class ProcessStarted
    {
        public ProcessStarted(string processId, ActorRef process)
        {
            this.ProcessId = processId;
            this.Process = process;
        }

        public string ProcessId { get; private set; }
        public ActorRef Process { get; private set; }
    }

    public class ProcessStopped
    {
        public ProcessStopped(string processId, ActorRef process)
        {
            this.ProcessId = processId;
            this.Process = process;
        }

        public string ProcessId { get; private set; }
        public ActorRef Process { get; private set; }
    }

    public abstract class ProcessManager : Actor
    {
        private IDictionary<String, ActorRef> processes = new Dictionary<String, ActorRef>();

        protected ProcessManager()
        {
        }

        protected ActorRef ProcessOf(string processId)
        {
            return processes[processId];
        }

        protected void StartProcess(string processId, ActorRef process)
        {
            if (!processes.ContainsKey(processId))
            {
                processes.Add(processId, process);

                Self.Tell(new ProcessStarted(processId, process), Self);
            }
        }
  
        protected void StopProcess(string processId)
        {
            if (processes.ContainsKey(processId))
            {
                var process = processes[processId];

                processes.Remove(processId);

                Self.Tell(new ProcessStopped(processId, process), Self);
            }
        }
    }

    /////////////////////////////////////////////////////////
    /////////////////////// LoanBroker
    /////////////////////////////////////////////////////////

    public class QuoteBestLoanRate
    {
        public QuoteBestLoanRate(string taxId, int amount, int termInMonths)
        {
            this.TaxId = taxId;
            this.Amount = amount;
            this.TermInMonths = termInMonths;
        }

        public string TaxId { get; private set; }
        public int Amount { get; private set; }
        public int TermInMonths { get; private set; }
    }

    public class BestLoanRateQuoted
    {
        public BestLoanRateQuoted(
            string bankId,
            string loanRateQuoteId,
            string taxId,
            int amount,
            int termInMonths,
            int creditScore,
            double interestRate)
        {
            this.BankId = bankId;
            this.LoanRateQuoteId = loanRateQuoteId;
            this.TaxId = taxId;
            this.Amount = amount;
            this.TermInMonths = termInMonths;
            this.CreditScore = creditScore;
            this.InterestRate = interestRate;
        }

        public int Amount { get; private set; }
        public string BankId { get; private set; }
        public int CreditScore { get; private set; }
        public double InterestRate { get; private set; }
        public string LoanRateQuoteId { get; private set; }
        public string TaxId { get; private set; }
        public int TermInMonths { get; private set; }
    }

    public class BestLoanRateDenied
    {
        public BestLoanRateDenied(
            string loanRateQuoteId,
            string taxId,
            int amount,
            int termInMonths,
            int creditScore)
        {
            this.LoanRateQuoteId = loanRateQuoteId;
            this.TaxId = taxId;
            this.Amount = amount;
            this.TermInMonths = termInMonths;
            this.CreditScore = creditScore;
        }

        public int Amount { get; private set; }
        public int CreditScore { get; private set; }
        public string LoanRateQuoteId { get; private set; }
        public string TaxId { get; private set; }
        public int TermInMonths { get; private set; }
    }

    public class LoanBroker : ProcessManager
    {
        private ActorRef[] banks;
        private ActorRef creditBureau;
        private AutoResetEvent termination;

        public LoanBroker(
            ActorRef creditBureau,
            ActorRef[] banks,
            AutoResetEvent termination)
        {
            this.creditBureau = creditBureau;
            this.banks = banks;
            this.termination = termination;
        }

        public void OnReceive(BankLoanRateQuoted message)
        {
            Console.WriteLine("LoanBroker: " + message);

            var process = ProcessOf(message.LoanQuoteReferenceId);
                
            process.Tell(
                new RecordLoanRateQuote(
                    message.BankId,
            	    message.BankLoanRateQuoteId,
                    message.InterestRate),
                Self);
        }

        public void OnReceive(CreditChecked message)
        {
            Console.WriteLine("LoanBroker: " + message);

            var process = ProcessOf(message.CreditProcessingReferenceId);

            process.Tell(
                new EstablishCreditScoreForLoanRateQuote(
    	            message.CreditProcessingReferenceId,
            	    message.TaxId,
                    message.Score),
                Self);
        }

        public void OnReceive(CreditScoreForLoanRateQuoteDenied message)
        {
            Console.WriteLine("LoanBroker: " + message);

            var process = ProcessOf(message.LoanRateQuoteId);

            process.Tell(new TerminateLoanRateQuote(), Self);

            termination.Set();

            var denied =
                new BestLoanRateDenied(
                    message.LoanRateQuoteId,
	                message.TaxId,
                    message.Amount,
                    message.TermInMonths,
            	    message.Score);

            Console.WriteLine("Would be sent to original requester: " + denied);
        }

        public void OnReceive(CreditScoreForLoanRateQuoteEstablished message)
        {
            Console.WriteLine("LoanBroker: " + message);

            foreach (var bank in banks)
            {
                bank.Tell(
                    new QuoteLoanRate(
                        message.LoanRateQuoteId,
                        message.TaxId,
                        message.CreditScore,
                        message.Amount,
                        message.TermInMonths),
                    Self);
            }
        }

        public void OnReceive(LoanRateBestQuoteFilled message)
        {
            Console.WriteLine("LoanBroker: " + message);

            StopProcess(message.LoanRateQuoteId);

            termination.Set();

            var best =
                new BestLoanRateQuoted(
                    message.BestBankLoanRateQuote.BankId,
                    message.LoanRateQuoteId,
                    message.TaxId,
                    message.Amount,
                    message.TermInMonths,
                    message.CreditScore,
                    message.BestBankLoanRateQuote.InterestRate);

            Console.WriteLine("Would be sent to original requester: " + best);
        }

        public void OnReceive(LoanRateQuoteRecorded message)
        {
            Console.WriteLine("LoanBroker: " + message);

            // Other processing...
        }

        public void OnReceive(LoanRateQuoteStarted message)
        {
            Console.WriteLine("LoanBroker: " + message);

            creditBureau.Tell(new CheckCredit(message.LoanRateQuoteId, message.TaxId), Self);
        }

        public void OnReceive(LoanRateQuoteTerminated message)
        {
            Console.WriteLine("LoanBroker: " + message);

            StopProcess(message.LoanRateQuoteId);
        }

        public void OnReceive(ProcessStarted message)
        {
            Console.WriteLine("LoanBroker: " + message);

            message.Process.Tell(new StartLoanRateQuote(banks.Length), Self);
        }

        public void OnReceive(ProcessStopped message)
        {
            Console.WriteLine("LoanBroker: " + message);

            Context.Stop(message.Process);
        }

        public void OnReceive(QuoteBestLoanRate message)
        {
            var loanRateQuoteId = Guid.NewGuid().ToString();

            Console.WriteLine("Starting: " + message + " for Id: " + loanRateQuoteId);

            ActorRef loanRateQuote =
                Context.ActorOf(
                    typeof(LoanRateQuote),
                    Props.With(
                        loanRateQuoteId,
                        message.TaxId,
                        message.Amount,
                        message.TermInMonths,
                        Self));

            StartProcess(loanRateQuoteId, loanRateQuote);
        }
    }

    /////////////////////////////////////////////////////////
    /////////////////////// LoanRateQuote
    /////////////////////////////////////////////////////////

    public class StartLoanRateQuote
    {
        public StartLoanRateQuote(int expectedLoanRateQuotes)
        {
            this.ExpectedLoanRateQuotes = expectedLoanRateQuotes;
        }

        public int ExpectedLoanRateQuotes { get; private set; }
    }
    
    public class LoanRateQuoteStarted
    {
        public LoanRateQuoteStarted(string loanRateQuoteId, string taxId)
        {
            this.LoanRateQuoteId = loanRateQuoteId;
            this.TaxId = taxId;
        }

        public string LoanRateQuoteId { get; private set; }
        public string TaxId { get; private set; }
    }

    public class TerminateLoanRateQuote
    {
    }
    
    public class LoanRateQuoteTerminated
    {
        public LoanRateQuoteTerminated(string loanRateQuoteId, string taxId)
        {
            this.LoanRateQuoteId = loanRateQuoteId;
            this.TaxId = taxId;
        }

        public string LoanRateQuoteId { get; private set; }
        public string TaxId { get; private set; }
    }

    public class EstablishCreditScoreForLoanRateQuote
    {
        public EstablishCreditScoreForLoanRateQuote(
            string loanRateQuoteId,
            string taxId,
            int score)
        {
            this.LoanRateQuoteId = loanRateQuoteId;
            this.TaxId = taxId;
            this.Score = score;
        }

        public string LoanRateQuoteId { get; private set; }
        public int Score { get; private set; }
        public string TaxId { get; private set; }
    }

    public class CreditScoreForLoanRateQuoteEstablished
    {
        public CreditScoreForLoanRateQuoteEstablished(
            string loanRateQuoteId,
            string taxId,
            int creditScore,
            int amount,
            int termInMonths)
        {
            this.LoanRateQuoteId = loanRateQuoteId;
            this.TaxId = taxId;
            this.CreditScore = creditScore;
            this.Amount = amount;
            this.TermInMonths = termInMonths;
        }

        public int Amount { get; private set; }
        public int CreditScore { get; private set; }
        public string LoanRateQuoteId { get; private set; }
        public string TaxId { get; private set; }
        public int TermInMonths { get; private set; }
    }

    public class CreditScoreForLoanRateQuoteDenied
    {
        public CreditScoreForLoanRateQuoteDenied(
            string loanRateQuoteId,
            string taxId,
            int amount,
            int termInMonths,
            int score)
        {
            this.LoanRateQuoteId = loanRateQuoteId;
            this.TaxId = taxId;
            this.Amount = amount;
            this.TermInMonths = termInMonths;
            this.Score = score;
        }

        public int Amount { get; private set; }
        public string LoanRateQuoteId { get; private set; }
        public int Score { get; private set; }
        public string TaxId { get; private set; }
        public int TermInMonths { get; private set; }
    }

    public class RecordLoanRateQuote
    {
        public RecordLoanRateQuote(string bankId, string bankLoanRateQuoteId, double interestRate)
        {
            this.BankId = bankId;
            this.BankLoanRateQuoteId = bankLoanRateQuoteId;
            this.InterestRate = interestRate;
        }

        public string BankId { get; private set; }
        public string BankLoanRateQuoteId { get; private set; }
        public double InterestRate { get; private set; }
    }

    public class LoanRateQuoteRecorded
    {
        public LoanRateQuoteRecorded(
            string loanRateQuoteId,
            string taxId,
            BankLoanRateQuote bankLoanRateQuote)
        {
            this.LoanRateQuoteId = loanRateQuoteId;
            this.TaxId = taxId;
            this.BankLoanRateQuote = BankLoanRateQuote;
        }

        public BankLoanRateQuote BankLoanRateQuote { get; private set; }
        public string LoanRateQuoteId { get; private set; }
        public string TaxId { get; private set; }
    }

    public class LoanRateBestQuoteFilled
    {
        public LoanRateBestQuoteFilled(
            string loanRateQuoteId,
            string taxId,
            int amount,
            int termInMonths,
            int creditScore,
            BankLoanRateQuote bestBankLoanRateQuote)
        {
            this.LoanRateQuoteId = loanRateQuoteId;
            this.TaxId = taxId;
            this.Amount = amount;
            this.TermInMonths = termInMonths;
            this.CreditScore = creditScore;
            this.BestBankLoanRateQuote = bestBankLoanRateQuote;
        }


        public int Amount { get; private set; }
        public BankLoanRateQuote BestBankLoanRateQuote { get; private set; }
        public int CreditScore { get; private set; }
        public string LoanRateQuoteId { get; private set; }
        public string TaxId { get; private set; }
        public int TermInMonths { get; private set; }
    }
    
    public class BankLoanRateQuote
    {
        public BankLoanRateQuote(string bankId, string bankLoanRateQuoteId, double interestRate)
        {
            this.BankId = bankId;
            this.BankLoanRateQuoteId = bankLoanRateQuoteId;
            this.InterestRate = interestRate;
        }

        public string BankId { get; private set; }
        public string BankLoanRateQuoteId { get; private set; }
        public double InterestRate { get; private set; }
    }

    public class LoanRateQuote : Actor
    {
        private IList<BankLoanRateQuote> bankLoanRateQuotes = new List<BankLoanRateQuote>();
        private int creditRatingScore;
        private int expectedLoanRateQuotes;

        private int amount;
        private ActorRef loanBroker;
        private string loanRateQuoteId;
        private string taxId;
        private int termInMonths;

        public LoanRateQuote(
            string loanRateQuoteId,
            string taxId,
            int amount,
            int termInMonths,
            ActorRef loanBroker)
        {
            this.loanRateQuoteId = loanRateQuoteId;
            this.taxId = taxId;
            this.amount = amount;
            this.termInMonths = termInMonths;
            this.loanBroker = loanBroker;
        }

        public void OnReceive(StartLoanRateQuote message)
        {
            expectedLoanRateQuotes = message.ExpectedLoanRateQuotes;

            loanBroker.Tell(new LoanRateQuoteStarted(loanRateQuoteId, taxId), Self);
        }

        public void OnReceive(EstablishCreditScoreForLoanRateQuote message)
        {
            creditRatingScore = message.Score;

            if (QuotableCreditScore(creditRatingScore))
            {
                loanBroker.Tell(
                    new CreditScoreForLoanRateQuoteEstablished(
                        loanRateQuoteId,
                        taxId,
                	    creditRatingScore,
                        amount,
                        termInMonths),
                    Self);
            }
            else
            {
                loanBroker.Tell(
                    new CreditScoreForLoanRateQuoteDenied(
                        loanRateQuoteId,
                        taxId,
                        amount,
                        termInMonths,
                        creditRatingScore),
                    Self);
            }
        }

        public void OnReceive(RecordLoanRateQuote message)
        {
            var bankLoanRateQuote =
    	            new BankLoanRateQuote(
  		                message.BankId,
  		                message.BankLoanRateQuoteId,
  		                message.InterestRate);

            bankLoanRateQuotes.Add(bankLoanRateQuote);

            loanBroker.Tell(
                new LoanRateQuoteRecorded(
                    loanRateQuoteId,
                    taxId,
                    bankLoanRateQuote),
                Self);

            if (bankLoanRateQuotes.Count >= expectedLoanRateQuotes)
            {
                loanBroker.Tell(
                    new LoanRateBestQuoteFilled(
                        loanRateQuoteId,
                        taxId,
                        amount,
                        termInMonths,
                        creditRatingScore,
                        BestBankLoanRateQuote()),
                    Self);
            }
        }

        public void OnReceive(TerminateLoanRateQuote message)
        {
            loanBroker.Tell(new LoanRateQuoteTerminated(loanRateQuoteId, taxId), Self);
        }

        private BankLoanRateQuote BestBankLoanRateQuote()
        {
            var best = bankLoanRateQuotes[0];

            foreach(var bankLoanRateQuote in bankLoanRateQuotes)
            {
                if (best.InterestRate > bankLoanRateQuote.InterestRate)
                {
                    best = bankLoanRateQuote;
                }
            }
    
            return best;
        }

        private bool QuotableCreditScore(int score)
        {
            return score > 399;
        }
    }

    /////////////////////////////////////////////////////////
    /////////////////////// CreditBureau
    /////////////////////////////////////////////////////////

    public class CheckCredit
    {
        public CheckCredit(
            string creditProcessingReferenceId,
            string taxId)
        {
            this.CreditProcessingReferenceId = creditProcessingReferenceId;
            this.TaxId = taxId;
        }

        public string CreditProcessingReferenceId { get; private set; }
        public string TaxId { get; private set; }
    }

    public class CreditChecked
    {
        public CreditChecked(
            string creditProcessingReferenceId,
            string taxId,
            int score)
        {
            this.CreditProcessingReferenceId = creditProcessingReferenceId;
            this.TaxId = taxId;
            this.Score = score;
        }

        public string CreditProcessingReferenceId { get; private set; }
        public int Score { get; private set; }
        public string TaxId { get; private set; }

    }

    public class CreditBureau : Actor
    {
        private int[] creditRanges = new int[] { 300, 400, 500, 600, 700 };
        private Random randomCreditRangeGenerator = new Random();
        private Random randomCreditScoreGenerator = new Random();

        public void OnReceive(CheckCredit message)
        {
            Console.WriteLine("CreditBureau: " + message);

            int range = creditRanges[randomCreditRangeGenerator.Next(5)];
            int score = range + randomCreditScoreGenerator.Next(20);

            CreditChecked creditChecked =
                new CreditChecked(
                    message.CreditProcessingReferenceId,
                    message.TaxId,
                    score);

            Console.WriteLine("CreditBureau: Telling: " + creditChecked);

            Sender.Tell(creditChecked, Self);
        }
    }

    /////////////////////////////////////////////////////////
    /////////////////////// Bank
    /////////////////////////////////////////////////////////

    public class QuoteLoanRate
    {
        public QuoteLoanRate(
            string loanQuoteReferenceId,
            string taxId,
            int creditScore,
            int amount,
            int termInMonths)
        {
            this.LoanQuoteReferenceId = loanQuoteReferenceId;
            this.TaxId = taxId;
            this.CreditScore = creditScore;
            this.Amount = amount;
            this.TermInMonths = termInMonths;
        }

        public int Amount { get; private set; }
        public int CreditScore { get; private set; }
        public string LoanQuoteReferenceId { get; private set; }
        public string TaxId { get; private set; }
        public int TermInMonths { get; private set; }
    }

    public class BankLoanRateQuoted
    {
        public BankLoanRateQuoted(
            string bankId,
            string bankLoanRateQuoteId,
            string loanQuoteReferenceId,
            string taxId,
            double interestRate)
        {
            BankId = bankId;
            BankLoanRateQuoteId = bankLoanRateQuoteId;
            LoanQuoteReferenceId = loanQuoteReferenceId;
            TaxId = taxId;
            InterestRate = interestRate;
        }

        public string BankId { get; private set; }
        public string BankLoanRateQuoteId { get; private set; }
        public string LoanQuoteReferenceId { get; private set; }
        public string TaxId { get; private set; }
        public double InterestRate { get; private set; }
    }


    public class Bank : Actor
    {
        private string bankId;
        private double primeRate;
        private Random randomDiscount = new Random();
        private Random randomQuoteId = new Random();
        private double ratePremium;

        public Bank(string bankId, double primeRate, double ratePremium)
        {
            this.bankId = bankId;
            this.primeRate = primeRate;
            this.ratePremium = ratePremium;
        }

        public void OnReceive(QuoteLoanRate message)
        {
            var interestRate =
                CalculateInterestRate(
                    (double) message.Amount,
                    (double) message.TermInMonths,
                    (double) message.CreditScore);

            var quoted =
                new BankLoanRateQuoted(
                    this.bankId,
                    this.randomQuoteId.Next(1000).ToString(),
                    message.LoanQuoteReferenceId,
                    message.TaxId,
                    interestRate);

            Sender.Tell(quoted, Self);
        }

        private double CalculateInterestRate(
          double amount,
          double months,
          double creditScore)
        {
            var creditScoreDiscount = creditScore / 100.0 / 10.0 - (this.randomDiscount.Next(5) * 0.05);

            return primeRate + ratePremium + ((months / 12.0) / 10.0) - creditScoreDiscount;
        }
    }
}
