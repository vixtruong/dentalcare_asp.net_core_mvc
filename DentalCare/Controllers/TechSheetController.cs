﻿using DentalCare.Models;
using DentalCare.Services;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace DentalCare.Controllers
{
    public class TechSheetController : Controller
    {
        private readonly DoctorService _doctorService;
        private readonly CustomerService _customerService;
        private readonly TechniqueService _techniqueService;
        private readonly TechWorkService _techWorkService;
        private readonly MedicalExamService _medicalExamService;
        private readonly TechDetailService _techDetailService;
        private readonly TechSheetService _techSheetService;

        public TechSheetController(DoctorService doctorService, CustomerService customerService, TechniqueService techniqueService, TechWorkService techWorkService, MedicalExamService medicalExamService, TechDetailService techDetailService, TechSheetService techSheetService)
        {
            _doctorService = doctorService;
            _customerService = customerService;
            _techniqueService = techniqueService;
            _techWorkService = techWorkService;
            _medicalExamService = medicalExamService;
            _techDetailService = techDetailService;
            _techSheetService = techSheetService;
        }

        public IActionResult Index(int? page)
        {
            var pageNumber = (page ?? 1);
            var pageSize = 10;

            ViewBag.Doctors = _doctorService.GetAll();
            ViewBag.Customers = _customerService.GetAll();
            ViewBag.MedicalExams = _medicalExamService.GetAll();

            var techSheets = _techSheetService.GetAll();
            var pagedList = techSheets.ToPagedList(pageNumber, pageSize);
            return View(pagedList);
        }

        [HttpGet]
        public JsonResult GetTechworkByType(string id)
        {
            var medicines = _techWorkService.GetByType(id);
            var medicineList = medicines.Select(d => new { id = d.Id, name = d.Name }).ToList();
            return Json(medicineList);
        }

        [HttpGet]
        public IActionResult Add()
        {
            ViewBag.MedicalExams = _medicalExamService.GetAll();
            ViewBag.Types = _techniqueService.GetAll();
            ViewBag.Medicines = _techWorkService.GetAll();
            return View();
        }

        [HttpPost]
        public IActionResult Add(TechSheetViewModel model)
        {
            if (model.Details.Count == 0)
            {
                TempData["ErrorDetailNullMessage"] = "List techworks is empty. Please choose techworks to add!";
                return RedirectToAction("Add");
            }

            if (_techSheetService.IsExistMes(model.MedicalExamId))
            {
                TempData["ErrorMessage"] = "A techsheet has already been created for this mes. Please edit if you want to change the techsheet information.";
                return RedirectToAction("Index");
            }

            var techsheet = new Techsheet
            {
                Id = _techSheetService.GenerateID(),
                Date = model.Date,
                MedicalexaminationId = model.MedicalExamId
            };
            _techSheetService.Add(techsheet);

            var techDetailList = new List<Techdetail>();
            foreach (var detail in model.Details)
            {
                var newDetail = new Techdetail
                {
                    Techpositionid = detail.TechworkId,
                    Quantity = short.Parse(detail.Quantity),
                    TechsheetId = techsheet.Id
                };

                techDetailList.Add(newDetail);
            }

            _techDetailService.AddRange(techDetailList);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            ViewBag.MedicalExams = _medicalExamService.GetAll();
            ViewBag.Types = _techniqueService.GetAll();
            ViewBag.Medicines = _techWorkService.GetAll();

            var prescription = _techSheetService.Get(id);
            var details = new List<TechDetailViewModel>();
            foreach (var detail in _techDetailService.GetAll())
            {
                if (detail.TechsheetId == id)
                {
                    details.Add(new TechDetailViewModel { TechworkId = detail.Techpositionid, Quantity = detail.Quantity.ToString() });
                }
            }

            var prescriptionViewModel = new TechSheetViewModel
            {
                Id = prescription.Id,
                MedicalExamId = prescription.MedicalexaminationId,
                Date = prescription.Date,
                Details = details
            };

            return View(prescriptionViewModel);
        }

        [HttpPost]
        public IActionResult Edit(TechSheetViewModel model)
        {
            var techsheet = _techSheetService.Get(model.Id);
            techsheet.MedicalexaminationId = model.MedicalExamId;
            techsheet.Date = model.Date;

            _techDetailService.DeleteRangeByTechsheetId(model.Id);

            var updateList = new List<Techdetail>();
            foreach (var detail in model.Details)
            {
                var updateDetail = new Techdetail
                {
                    TechsheetId = techsheet.Id,
                    Techpositionid = detail.TechworkId,
                    Quantity = short.Parse(detail.Quantity)
                };
                updateList.Add(updateDetail);
            }

            _techDetailService.AddRange(updateList);

            return RedirectToAction("Index");
        }

        public IActionResult Delete(string id)
        {
            foreach (var detail in _techDetailService.GetAll())
            {
                if (id == detail.TechsheetId)
                {
                    TempData["ErrorMessage"] = "This techsheet relate to your business database! Can not delete!";
                    return RedirectToAction("Index");
                }
            }

            _techSheetService.Delete(id);

            return RedirectToAction("Index");
        }
    }
}
