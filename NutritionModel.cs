using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosSample
{

    public class NutritionSummaryModel
    {
        public string id { get; set; }

        public string createdBy { get; set; }


        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class NutritionModel
    {
        public NutritionModel(){
            this.tags = new List<Tag>();
            this.nutrients = new List<Nutrient>();
            this.servings = new List<Serving>();
            this.history = new History();
          }
        public string id { get; set; }

        public string description { get; set; }

        public string descriptionLowerCase { get; set; }

        public int version { get; set; }

        public string foodGroup { get; set; }

        public IEnumerable<Tag> tags { get; set; }

        public IEnumerable<Nutrient> nutrients { get; set; }

        public IEnumerable<Serving> servings { get; set; }

        public History history { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class History
    {
        public string createdBy { get; set; }
        public string createdByToLower { get; set; }
        public DateTime createdDate { get; set; }
    }

    public class Tag
    {
        public string name { get; set; }
    }

    public class Nutrient
    {
        public string id { get; set; }
        public string description { get; set; }
        public int nutrionValue { get; set; }
        public string units { get; set; }

    }

    public class Serving
    {
        public int amount { get; set; }
        public string description { get; set; }
        public decimal weightInGrams { get; set; }

    }
}
