﻿using Application.Common.Models;
using Application.Common.Interfaces;
using Application.Options;
using Application.VaccineCredential.Queries.GetVaccineStatus;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;
using Snowflake.Data.Client;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Security;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Application.Common
{
    public class Utils
    {
        public static readonly ImmutableDictionary<string, string> VaccineTypeNames = new Dictionary<string, string>
        {
            { "207", "Moderna" },
            { "208", "Pfizer" },
            { "210", "AstraZeneca" },
            { "211", "Novavax" },
            { "212", "J&J" },
            { "213", "COVID-19, unspecified" },
            { "217", "Pfizer" },
            { "218", "Pfizer" }
        }.ToImmutableDictionary();

        private readonly AppSettings _appSettings;
        private static int messageCalls = 0;
        public Utils(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }
        public static int ValidatePin(string pin)
        {
            //Business Rules:  1)  4 digit pin
            //    2)  Not 4 or more of the same # eg:  0000,1111 (not allow)
            //    3)  No more than 3 consecutive #, eg: 1234 (not allow)
            if (pin == null)
            {
                return 1;
            }
            if (pin.Length != 4)
            {
                return 2;
            }

            if (!Int32.TryParse(pin, out _))
            {
                return 3;
            }

            //    2)  Not 4 or more of the same # eg:  1111 (not allow)
            if (ContainsDupsChars(pin, 4))
            {
                return 4;
            }

            //   3)  No consecutive #, eg: 1234 (not allow)
            if (HasConsecutive(pin, 4))
            {
                return 5;
            }

            return 0;
        }

        public static bool ContainsDupsChars(string s, int max)
        {
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            int cnt = 0;
            for (int i = 0; i < s.Length - 1 && cnt < max; ++i)
            {
                var charI = s[i];
                cnt = s.Count(c => c == charI);
                if (cnt >= max)
                {
                    return true;
                }
            }

            return false;
        }


        public static bool HasConsecutive(string s, int max)
        {
            int cnt = 0;
            for (int i = 0; i < s.Length - 1; i++)
            {
                var chr1 = s[i];
                var chr2 = s[i + 1];
                if (chr1 + 1 == chr2)
                {
                    cnt++;
                }
                else
                {
                    cnt = 0;
                }
                if (cnt >= max - 1)
                {
                    break;
                }

            }

            return cnt >= max - 1;
        }

        public static string ParseLotNumber(string s)
        {   // Check if lot number is Alpha numeric
            if (s == null) {  return null; }                       
            
            var regex = "^[a-zA-Z0-9-]*$";
            var regexContainsNumber = "[\\d]";
            var tokens = s.Split(" ");

            foreach(var t in tokens)
            {
                if(Regex.IsMatch(t, regex) && Regex.IsMatch(t, regexContainsNumber))
                {
                    return t;
                }
            }

            return null;
        }

        public static string TrimString(string s, int i)
        {
            if (s == null) {  return null; }

            if(s.Length > i)
            {
                s = s.Substring(0, i);
            }
            return s;
        }

        //returns 0 if cached
        //        1 if not in db
        //        2 if email send success
        //        3 if sms send successs
        //        4 if email error
        //        5 is sms error
        //        6 in db sms failed for invalid phone
        //        7 if not in db and sms failed
        //        8 if not in db and email Failed
        public async Task<int> ProcessStatusRequest(ILogger logger, IEmailService _emailService, SendGridSettings _sendGridSettings, IMessagingService _messagingService, IAesEncryptionService _aesEncryptionService, GetVaccineCredentialStatusQuery request, ISnowFlakeService _snowFlakeService, SnowflakeDbConnection conn,  CancellationToken cancellationToken, long tryCount = 1)
        {
            Interlocked.Increment(ref messageCalls);
            int ret = 0;
            var smsRecipient = request.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(_appSettings.DeveloperSms))
            {
                smsRecipient = _appSettings.DeveloperSms;
                logger.LogInformation($"ProcessStatusRequest: sending to developer SMS instead of request.PhoneNumber");
            }
            var emailRecipient = request.EmailAddress;
            if (!string.IsNullOrWhiteSpace(_appSettings.DeveloperEmail))
            {
                emailRecipient = _appSettings.DeveloperEmail;
                logger.LogInformation($"ProcessStatusRequest: sending to developer email instead of request.EmailAddress");
            }

            // Get Vaccine Credential
            string response;
            if (conn == null)
            {
                response = await _snowFlakeService.GetVaccineCredentialStatusAsync(request, cancellationToken);
            }
            else
            {
                response = await _snowFlakeService.GetVaccineCredentialStatusAsync(conn, request, cancellationToken);
            }

            var logMessage = $"searchCriteria:{Sanitize(request.FirstName.Substring(0, 1))}.{Sanitize(request.LastName.Substring(0, 1))}. response:{Sanitize(response)}";

            if (!string.IsNullOrEmpty(response))
            {
                //Generate link url with the GUID and send text or email based on the request preference.
                //Encyrpt the response with  aesencrypt 
                var code = DateTime.Now.Ticks + "~" + request.Pin + "~" + response;
                var encrypted = _aesEncryptionService.EncryptGcm(code, _appSettings.CodeSecret);
                logMessage = $"{logMessage} code:{encrypted}";

                var url = $"{_appSettings.WebUrl}/qr/{request.Language}/{encrypted}";

                //SMS.
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    ret = 3;
                   
                    var messageId = await _messagingService.SendMessageAsync(smsRecipient, FormatSms(Convert.ToInt32(_appSettings.LinkExpireHours), url, request.Language), request.Language, cancellationToken);
                    
                    if (string.IsNullOrEmpty(messageId))
                    {
                        ret = 5;
                    }
                    else if (messageId == "BADNUMBER")
                    {
                        ret = 6;
                    }
                }

                //Email
                if (!string.IsNullOrEmpty(request.EmailAddress))
                {
                    ret = 2;
                    
                    var message = new EmailRequest
                    {
                        RecipientEmail = emailRecipient,
                        RecipientName = $"{UppercaseFirst(request.FirstName)} {UppercaseFirst(request.LastName)}",
                        Subject = "Digital COVID-19 Vaccine Record - QR code available",
                        HtmlContent = FormatHtml(url, request.Language, Convert.ToInt32(_appSettings.LinkExpireHours)),
                        PlainTextContent = FormatSms(Convert.ToInt32(_appSettings.LinkExpireHours), url, request.Language)

                    };

                    if (string.IsNullOrEmpty(await _emailService.SendEmailAsync(message, cancellationToken)))
                    {
                        ret = 4; // Failed to send email
                    }                    
                }
            }
            else  //Not matched
            {
                ret = 1;
                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    logMessage = $"RQ_EMAIL {logMessage}";
                }
                else
                {
                    logMessage = $"RQ_SMS {logMessage}"; 
                }

                //Email sms request that we could not find you
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    if (_appSettings.SendNotFoundSms != "0")
                    {
                        var messageId = await _messagingService.SendMessageAsync(smsRecipient, FormatNotFoundSms(request.Language), request.Language, cancellationToken);
                        
                        if (string.IsNullOrEmpty(messageId))
                        {
                            ret = 7;
                        }
                    }
                }
                else
                {
                    if (_appSettings.SendNotFoundEmail != "0")
                    {
                        var message = new EmailRequest
                        {
                            RecipientEmail = emailRecipient,
                            RecipientName = $"{UppercaseFirst(request.FirstName)} {UppercaseFirst(request.LastName)}",
                            Subject = "Digital COVID-19 Vaccine Record - Record not found",
                            HtmlContent = FormatNotFoundHtml(request.Language),
                            PlainTextContent = FormatNotFoundSms(request.Language),
                        };

                        if (string.IsNullOrEmpty(await _emailService.SendEmailAsync(message, cancellationToken)))
                        {
                            ret = 8; // Failed to send email
                        }
                    }
                }
            }
            if (tryCount <= 1)
            {
                switch (ret)
                {
                    case 0:
                        logger.LogInformation($"CACHEDREQUEST {logMessage}.");
                        break;
                    case 1:
                        logger.LogInformation($"BADREQUEST-NOTFOUND {logMessage}.");
                        break;
                    case 2:
                        logger.LogInformation($"VALIDREQUEST-EMAILSENT {logMessage}.");
                        break;
                    case 3:
                        logger.LogInformation($"VALIDREQUEST-SMSSENT {logMessage}.");
                        break;
                    case 4:
                        logger.LogWarning($"VALIDREQUEST-EMAILFAILED {logMessage}.");
                        break;
                    case 5:
                    case 6:
                        logger.LogWarning($"VALIDREQUEST-SMSFAILED {logMessage}.");
                        break;
                    case 7:
                        logger.LogWarning($"BADREQUEST-SMSFAILED {logMessage}.");
                        break;
                    case 8:
                        logger.LogWarning($"BADREQUEST-EMAILFAILED {logMessage}.");
                        break;
                    default:
                        break;

                }
            }
            else
            {
                logger.LogInformation($"ProcessStatus retry cnt={tryCount} ret={ret} {logMessage}.");
            }

            return ret;
        }
        /*
         es: Spanish
         zh: Chinese Simplified
         zh-TW: Chinese Traditional
         ko: Korean
         vi: Vietnamese
         ar: Arabic
         tl: Tagalog
         */
        public static string FormatSms(int linkExpireHours, string url, string lang)
        {
            return lang switch
            {
                "es" => $"Gracias por visitar el portal del Registro digital de vacunación contra el COVID-19 del estado de California. El enlace para recuperar su código de registro digital de vacunación tiene una validez de {linkExpireHours} horas. Una vez accedido y guardado en su dispositivo, el código QR no expirará.\nVer el registro de vacunación:\n{url}",
                "cn" or "zh" => $"感谢访问加利福尼亚州数字新冠肺炎疫苗接种记录门户网站。检索您的数字疫苗接种记录的链接在 {linkExpireHours} 小时内有效。访问并保存到设备后，二维码将不会过期。\n查看疫苗接种记录：\n{url}",
                "tw" or "zh-TW" => $"感謝您造訪加州的數位 COVID-19 疫苗接種記錄入口網站。用來擷取疫苗接種數位記錄之連結的有效期間為 {linkExpireHours} 小時。存取並儲存到裝置後，QR 代碼就不會過期。\n檢視疫苗接種記錄：\n{url}",
                "kr" or "ko" => $"캘리포니아주의 디지털 코로나19 백신 기록 포털에 방문해 주셔서 감사합니다. 디지털 백신 기록을 되찾을 수 있는 링크는 {linkExpireHours}시간 동안 유효합니다. 액세스하여 기기에 저장한 QR 코드는 만료되지 않습니다.\n백신 기록 보기:\n{url}",
                "vi" => $"Cảm ơn quý vị đã truy cập vào cổng thông tin Hồ sơ Vắc xin COVID-19 Kỹ thuật số của Tiểu bang California. Đường liên kết để truy xuất mã hồ sơ vắc xin kỹ thuật số của quý vị chỉ có hiệu lực trong {linkExpireHours} giờ. Sau khi truy cập và lưu vào thiết bị, mã QR của quý vị sẽ không hết hạn.\nXem Hồ sơ Vắc xin:\n{url}",
                "ae" or "ar" => $"نشكرك على زيارة البوابة الإلكترونية للسجل الرقمي للقاح فيروس كورونا (كوفيد-19) بولاية كاليفورنيا. إن الرابط الخاص باسترداد سجل اللقاح الرقمي الخاص بك صالح لمدة {linkExpireHours} ساعة. بمجرد الوصول إليه وحفظه على جهازك، لن تنتهي صلاحية رمز الاستجابة السريعة.\nعرض سجل اللقاح:\n{url}",
                "ph" or "tl" => $"Salamat sa pagbisita sa portal ng Digital na Rekord ng Pagpapabakuna laban sa COVID-19 ng Estado ng California. {linkExpireHours} na oras lang may bisa ang link para kunin ang iyong digital na rekord ng pagpapabakuna. Kapag na-access na at na-save sa iyong device, hindi mag-e-expire ang QR code.\nTingnan ang Rekord ng Bakuna:\n{url}",
                _ => $"Thank you for visiting the State of California's Digital COVID-19 Vaccine Record portal. The link to retrieve your digital vaccine record is valid for {linkExpireHours} hours. Once accessed and saved to your device, the QR code will not expire.\nView Vaccine Record:\n{url}",
            };
        }

        public string FormatHtml(string url, string lang, int linkExpireHours)
        {
            return lang switch
            {
                "es" => $"<img src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png'><br/>" +
                            $"<h3 style='color: #f06724'>Registro digital de vacunación contra el COVID-19</h3>" +
                            $"<p>Gracias por visitar el portal del Registro digital de vacunación contra el COVID-19 del estado de California. El enlace para recuperar su código de registro digital de vacunación tiene una validez de {linkExpireHours} horas. Una vez accedido y guardado en su dispositivo, el código QR no expirará.</p>" +
                            $"<p><a href='{url}'>Ver el registro digital de vacunación</a></p>" +
                            $"<p>Obtenga más información sobre cómo <a href='{_appSettings.CDCUrl}'>protegerse y proteger a los demás</a> de parte de los Centros para el Control y la Prevención de Enfermedades. </p>" +
                            $"<p><b>¿Tiene preguntas?</b></p>" +
                            $"<p><a href='{_appSettings.VaccineFAQUrl}'>Visite nuestra página de preguntas frecuentes</a> para obtener más información sobre su registro digital de vacunación contra el COVID-19.</p>" +
                            $"<p><b>Manténgase informado.</b></p>" +
                            $"<p><a href='{_appSettings.CovidWebUrl}'>Consulte la información más reciente</a> sobre el COVID-19 en California.</p><br/>" +
                            $"<hr>" +
                            $"<footer><p style='text-align:center'>Correo electrónico oficial del Departamento de Estado de California</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>",
                "cn" or "zh" => $"<img src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png'><br/>" +
                            $"<h3 style='color: #f06724'>数字新冠肺炎疫苗接种记录</h3>" +
                            $"<p>感谢访问加利福尼亚州数字新冠肺炎疫苗接种记录门户网站。检索您的数字疫苗接种记录的链接在 {linkExpireHours} 小时内有效。访问并保存到设备后，二维码将不会过期。</p>" +
                            $"<p><a href='{url}'>查看疫苗接种记录</a></p>" +
                            $"<p>从疾病控制和预防中心了解有关如何<a href='{_appSettings.CDCUrl}'>保护自己和他人</a>的更多信息。</p>" +
                            $"<p><b>有问题吗？</b></p>" +
                            $"<p><a href='{_appSettings.VaccineFAQUrl}'>访问我们的常见问题页面</a> 以了解有关您的数字新冠肺炎疫苗接种记录的更多信息。</p>" +
                            $"<p><b>保持关注。</b></p>" +
                            $"<p><a href='{_appSettings.CovidWebUrl}'>查看有关加利福尼亚州新冠肺炎疫情的</a>最新信息。</p><br/>" +
                            $"<hr>" +
                            $"<footer><p style='text-align:center'>加州事务部官方电子邮件</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>",
                "tw" or "zh-TW" => $"<img src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png'><br/>" +
                            $"<h3 style='color: #f06724'>數位 COVID-19 疫苗接種記錄</h3>" +
                            $"<p>感謝您造訪加州的數位 COVID-19 疫苗接種記錄入口網站。用來擷取疫苗接種數位記錄之連結的有效期間為 {linkExpireHours} 小時。存取並儲存到裝置後，QR 代碼就不會過期。</p>" +
                            $"<p><a href='{url}'>檢視疫苗接種數位記錄</a></p>" +
                            $"<p>透過疾病管制與預防中心進一步瞭解如何<a href='{_appSettings.CDCUrl}'>保護自己和他人。</a></p>" +
                            $"<p><b>有問題嗎？</b></p>" +
                            $"<p><a href='{_appSettings.VaccineFAQUrl}'>造訪常見問題頁面</a>，進一步瞭解您的 COVID-19 疫苗接種數位記錄。</p>" +
                            $"<p><b>隨時注意最新資訊。</b></p>" +
                            $"<p><a href='{_appSettings.CovidWebUrl}'>檢視加州關於</a> COVID-19 的最新資訊。</p><br/>" +
                            $"<hr>" +
                            $"<footer><p style='text-align:center'>官方加州部門電子郵件</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>",
                "kr" or "ko" => $"<img src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png'><br/>" +
                            $"<h3 style='color: #f06724'>디지털 코로나19 백신 기록</h3>" +
                            $"<p>캘리포니아주의 디지털 코로나19 백신 기록 포털에 방문해 주셔서 감사합니다. 디지털 백신 기록을 되찾을 수 있는 링크는 {linkExpireHours}시간 동안 유효합니다. 액세스하여 기기에 저장한 QR 코드는 만료되지 않습니다.</p>" +
                            $"<p><a href='{url}'>디지털 백신 기록 보기</a></p>" +
                            $"<p><a href='{_appSettings.CDCUrl}'>스스로와 다른 사람을 보호하는</a> 방법에 대해 질병통제예방센터(CDC)에서 자세히 알아보십시오.</p>" +
                            $"<p><b>질문이 있습니까?</b></p>" +
                            $"<p><a href='{_appSettings.VaccineFAQUrl}'>FAQ 페이지에서</a> 디지털 COVID-19 백신 접종 기록에 대해 더 알아보십시오.</p>" +
                            $"<p><b>최신 정보를 얻으십시오.</b></p>" +
                            $"<p><a href='{_appSettings.CovidWebUrl}'>COVID-19 in California에서</a>최신 정보를 보십시오.</p><br/>" +
                            $"<hr>" +
                            $"<footer><p style='text-align:center'>공식 캘리포니아주 행정청 이메일</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>",
                "vi" => $"<img src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png'><br/>" +
                            $"<h3 style='color: #f06724'>Hồ sơ Vắc xin COVID-19 Kỹ thuật số</h3>" +
                            $"<p>Cảm ơn quý vị đã truy cập vào cổng thông tin Hồ sơ Vắc xin COVID-19 Kỹ thuật số của Tiểu bang California. Đường liên kết để truy xuất mã hồ sơ vắc xin kỹ thuật số của quý vị chỉ có hiệu lực trong {linkExpireHours} giờ. Sau khi truy cập và lưu vào thiết bị, mã QR của quý vị sẽ không hết hạn.</p>" +
                            $"<p><a href='{url}'>Xem hồ sơ vắc xin kỹ thuật số</a></p>" +
                            $"<p>Tìm hiểu thêm về cách <a href='{_appSettings.CDCUrl}'>bảo vệ bản thân và người khác</a> từ Trung tâm Kiểm soát và Phòng ngừa Dịch bệnh. </p>" +
                            $"<p><b>Nếu quý vị có thắc mắc?</b></p>" +
                            $"<p><a href='{_appSettings.VaccineFAQUrl}'>Truy cập trang Các Câu hỏi Thường Gặp</a> để tìm hiểu thêm về Hồ sơ Vắc xin COVID-19 Kỹ thuật số của quý vị.</p>" +
                            $"<p><b>Hãy luôn Cập nhật.</b></p>" +
                            $"<p><a href='{_appSettings.CovidWebUrl}'>Xem thông tin mới nhất</a> về COVID-19 ở California.</p><br/>" +
                            $"<hr>" +
                            $"<footer><p style='text-align:center'>Email chính thức của Tiểu bang California</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>",
                "ae" or "ar" => $"<img dir='rtl' src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png'><br/>" +
                            $"<h3 dir='rtl' style='color: #f06724'>السجل الرقمي للقاح فيروس كورونا (كوفيد-19)</h3>" +
                            $"<p dir='rtl'>نشكرك على زيارة البوابة الإلكترونية للسجل الرقمي للقاح فيروس كورونا (كوفيد-19) بولاية كاليفورنيا. إن الرابط الخاص باسترداد سجل اللقاح الرقمي الخاص بك صالح لمدة {linkExpireHours} ساعة. بمجرد الوصول إليه وحفظه على جهازك، لن تنتهي صلاحية رمز الاستجابة السريعة.</p>" +
                            $"<p dir='rtl'><a href='{url}'>عرض سجل اللقاح الرقمي</a></p>" +
                            $"<p dir='rtl'>تعرَّف على المزيد حول <a href='{_appSettings.CDCUrl}'></a> من مراكز مكافحة الأمراض والوقاية منها. </p>" +
                            $"<p dir='rtl'><b>هل لديك أي أسئلة؟</b></p>" +
                            $"<p dir='rtl'><a href='{_appSettings.VaccineFAQUrl}'>تفضل بزيارة صفحة الأسئلة الشائعة لدينا</a > لمعرفة المزيد حول سجل اللقاح الرقمي لكوفيد-19 الخاص بك.</p>" +
                            $"<p dir='rtl'><b>ابقَ على اطلاع.</b></p>" +
                            $"<p dir='rtl'><a href='{_appSettings.CovidWebUrl}'>اطلع على أحدث معلومات</a> كوفيد-19 في كاليفورنيا.</p>" +
                            $"<hr>" +
                            $"<footer><p dir='rtl' style='text-align:center'>البريد الإلكتروني الرسمي لوزارة الخارجية الخاص بولاية كاليفورنيا</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>",
                "ph" or "tl" => $"<img src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png'><br/>" +
                            $"<h3 style='color: #f06724'>Digital na Rekord ng Bakuna para sa COVID-19</h3>" +
                            $"<p>Salamat sa pagbisita sa portal ng Digital na Rekord ng Pagpapabakuna laban sa COVID-19 ng Estado ng California. {linkExpireHours} na oras lang may bisa ang link para kunin ang iyong digital na rekord ng pagpapabakuna. Kapag na-access na at na-save sa iyong device, hindi mag-e-expire ang QR code.</p>" +
                            $"<p><a href='{url}'>Tingnan ang digital na rekord ng pagpapabakuna</a></p>" +
                            $"<p>Matuto pa mula sa Centers for Disease Control and Prevention kung paano <a href='{_appSettings.CDCUrl}'>mapoprotektahan ang iyong sarili at ang ibang tao.</a></p>" +
                            $"<p><b>May mga tanong?</b></p>" +
                            $"<p><a href='{_appSettings.VaccineFAQUrl}'>Bisitahin ang aming page ng FAQ</a> para alamin pa ang tungkol sa iyong Digital na Record ng Bakuna sa COVID-19.</p>" +
                            $"<p><b>Makibalita.</b></p>" +
                            $"<p><a href='{_appSettings.CovidWebUrl}'>Tingnan ang pinakabagong impormasyon</a> sa COVID-19 sa California.</p><br/>" +
                            $"<hr>" +
                            $"<footer><p style='text-align:center'>Opisyal na Email ng Departamento ng Estado ng California</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>",
                _ => $"<img src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png'><br/>" +
                            $"<h3 style='color: #f06724'>Digital COVID-19 Vaccine Record</h3>" +
                            $"<p>Thank you for visiting the State of California's Digital COVID-19 Vaccine Record portal. The link to retrieve your digital vaccine record is valid for {linkExpireHours} hours. Once accessed and saved to your device, the QR code will not expire.</p>" +
                            $"<p><a href='{url}'>View digital vaccine record</a></p>" +
                            $"<p>Learn more about how to <a href='{_appSettings.CDCUrl}'>protect yourself and others</a> from the Centers for Disease Control and Prevention.</p>" +
                            $"<p><b>Have questions?</b></p>" +
                            $"<p><a href='{_appSettings.VaccineFAQUrl}'>Visit our FAQ page</a> to learn more about your Digital COVID-19 Vaccine Record.</p>" +
                            $"<p><b>Stay Informed.</b></p>" +
                            $"<p><a href='{_appSettings.CovidWebUrl}'>View the latest information</a> on COVID-19 in California.</p><br/>" +
                            $"<hr>" +
                            $"<footer><p style='text-align:center'>Official California State Department Email</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>"
            };
        }

        public string FormatNotFoundSms(string lang)
        {
            return lang switch
            {
                "es" => $"Hace poco solicitó un registro digital de vacunación contra el COVID-19. Sin embargo, la información que proporcionó no coincide con la información que tenemos en el sistema. Envíe otra solicitud con un número de teléfono celular o una dirección de correo electrónico diferentes a MyVaccineRecord.CDPH.ca.gov. O para obtener ayuda para hacer que su registro de vacunación coincida con su información de contacto, use nuestro asistente virtual:\n{_appSettings.VirtualAssistantUrl}",
                "cn" or "zh" => $"您最近请求了数字新冠肺炎疫苗接种记录。很遗憾，您提供的信息与系统中的信息不符。使用不同的手机号码或电子邮件地址在 MyVaccineRecord.CDPH.ca.gov 重新提交请求。或者使用我们的虚拟助手获取帮助，以便将您的记录与您的联系信息进行匹配：\n{_appSettings.VirtualAssistantUrl}",
                "tw" or "zh-TW" => $"您最近申請了 COVID-19 疫苗接種數位記錄。不過，您提供的資訊與我們系統的資訊不相符。請在 MyVaccineRecord.CDPH.ca.gov 以其他手機號碼或電子郵件地址另外提交申請。或取得協助比對您的疫苗接種記錄與聯絡資訊，請使用虛擬助理：\n{_appSettings.VirtualAssistantUrl}",
                "kr" or "ko" => $"최근에 디지털 코로나19 백신 기록을 신청하셨습니다. 하지만 귀하가 제공한 정보는 당사 시스템의 정보와 일치하지 않습니다. MyVaccineRecord.CDPH.ca.gov에서 다른 모바일 전화번호 또는 이메일 주소로 요청을 다시 제출하십시오. 연락처 정보와 일치하는 백신 기록을 얻는데 도움을 받으려면 가상 도우미{_appSettings.VirtualAssistantUrl}를 사용하십시오.",
                "vi" => $"Quý vị đang yêu cầu Hồ sơ Vắc xin COVID-19 Kỹ thuật số. Tuy nhiên, thông tin mà quý vị cung cấp không có trên hệ thống. Hãy gửi yêu cầu khác bằng số điện thoại di động hoặc địa chỉ email khác tại MyVaccineRecord.CDPH.ca.gov. Hoặc sử dụng Trợ lý Ảo của chúng tôi để được giúp tìm hồ sơ vắc xin bằng thông tin mà quý vị cung cấp:\n{_appSettings.VirtualAssistantUrl}",
                "ae" or "ar" => $"لقد طلبت مؤخرًا سجلًا رقميًا للقاح كوفيد-19. ومع ذلك، لا تتطابق المعلومات التي قدمتها مع المعلومات الموجودة في نظامنا. أرسل طلبًا آخر باستخدام رقم هاتف محمول مختلف أو عنوان بريد إلكتروني مختلف على MyVaccineRecord.CDPH.ca.gov. أو للحصول على المساعدة في مطابقة سجل اللقاح الخاص بك بمعلومات التواصل الخاصة بك، استخدم المساعد الافتراضي:\n{_appSettings.VirtualAssistantUrl}",
                "ph" or "tl" => $"Kamakailan kang humiling ng Digital na Rekord ng Pagpapabakuna Laban sa COVID-19. Gayunpaman, hindi tumutugma sa impormasyon sa aming system ang impormasyong ibinigay mo. Magsumite ng isa pang kahilingan gamit ang ibang numero ng mobile phone o email address sa  MyVaccineRecord.CDPH.ca.gov. O upang makahingi ng tulong sa pagtutugma ng iyong rekord sa iyong impormasyon sa pakikipag-ugnayan, gamitin ang aming Virtual Assistant:\n{_appSettings.VirtualAssistantUrl}",
                _ => $"You recently requested a Digital COVID-19 Vaccine Record. However, the information you provided does not match the information in our system. Submit another request with a different mobile phone number or email address at MyVaccineRecord.cdph.ca.gov. Or to get help matching your vaccine record to your contact information, use our Virtual Assistant: \n{_appSettings.VirtualAssistantUrl}"
            };
        }

        public string FormatNotFoundHtml(string lang)
        {
            return lang switch
            {
                "es" => $"<img src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png'><br/>" +
                            $"<h3 style='color: #f06724'>Registro digital de vacunación contra el COVID-19</h3>" +
                            $"<p>Hace poco solicitó un registro digital de vacunación contra el COVID-19 a <a href='{_appSettings.WebUrl}'>MyVaccineRecord.CDPH.ca.gov</a>. Lamentablemente, la información que proporcionó no coincide con la información que tenemos en el sistema. Puede <a href='{_appSettings.WebUrl}'>enviar otra solicitud</a> con un número de teléfono o una dirección de correo electrónico diferentes, o puede comunicarse con el <a href='{_appSettings.VirtualAssistantUrl}'>asistente virtual para COVID-19 del CDPH</a> con el fin de obtener ayuda para hacer que su registro coincida con su información de contacto.</p><br/>" +
                            $"<p><b>¿Tiene preguntas?</b></p>" +
                            $"<p><a href='{_appSettings.VaccineFAQUrl}'>Visite nuestra página de preguntas frecuentes</a> para obtener más información sobre su registro digital de vacunación contra el COVID-19.</p>" +
                            $"<p><b>Manténgase informado.</b></p>" +
                            $"<p><a href='{_appSettings.CovidWebUrl}'>Consulte la información más reciente</a> sobre el COVID-19 en California.</p><br/>" +
                            $"<hr>" +
                            $"<footer><p style='text-align:center'>Correo electrónico oficial del Departamento de Estado de California</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>",
                "cn" or "zh" => $"<img src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png'><br/>" +
                            $"<h3 style='color: #f06724'>数字新冠肺炎疫苗接种记录</h3>" +
                            $"<p>您最近从 <a href='{_appSettings.WebUrl}'>MyVaccineRecord.CDPH.ca.gov</a> 请求了数字新冠肺炎疫苗接种记录。很遗憾，您提供的信息与系统中的信息不符。您可以使用不同的手机号码或电子邮件地址<a href='{_appSettings.WebUrl}'>提交另一个请求</a>，或者您可以联系 <a href='{_appSettings.VirtualAssistantUrl}'>CDPH 新冠肺炎虚拟助手</a>以帮助将您的记录与您的联系信息进行匹配。</p><br/>" +
                            $"<p><b>有问题吗？</b></p>" +
                            $"<p><a href='{_appSettings.VaccineFAQUrl}'>访问我们的常见问题页面</a> 以了解有关您的数字新冠肺炎疫苗接种记录的更多信息。</p>" +
                            $"<p><b>保持关注。</b></p>" +
                            $"<p><a href='{_appSettings.CovidWebUrl}'>查看有关加利福尼亚州新冠肺炎疫情的</a>最新信息。</p><br/>" +
                            $"<hr>" +
                            $"<footer><p style='text-align:center'>加州事务部官方电子邮件</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>",
                "tw" or "zh-TW" => $"<img src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png'><br/>" +
                            $"<h3 style='color: #f06724'>數位 COVID-19 疫苗接種記錄</h3>" +
                            $"<p>您最近從 <a href='{_appSettings.WebUrl}'>MyVaccineRecord.CDPH.ca.gov</a> 申請了 COVID-19 疫苗接種數位記錄很遺憾，您提供的資訊與我們系統的資訊不相符。您可以使用不同的手機號碼或電子郵件地址<a href='{_appSettings.WebUrl}'>提交另一次申請</a>，或者可以聯絡 <a href='{_appSettings.VirtualAssistantUrl}'>CDPH COVID-19 虛擬助理</a>協助比對您的記錄與聯絡資訊。</p><br/>" +
                            $"<p><b>有問題嗎？</b></p>" +
                            $"<p><a href='{_appSettings.VaccineFAQUrl}'>造訪常見問題頁面</a>，進一步瞭解您的 COVID-19 疫苗接種數位記錄。</p>" +
                            $"<p><b>隨時注意最新資訊。</b></p>" +
                            $"<p><a href='{_appSettings.CovidWebUrl}'>檢視加州關於</a> COVID-19 的最新資訊。</p><br/>" +
                            $"<hr>" +
                            $"<footer><p style='text-align:center'>官方加州部門電子郵件</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>",
                "kr" or "ko" => $"<img src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png'><br/>" +
                            $"<h3 style='color: #f06724'>디지털 코로나19 백신 기록</h3>" +
                            $"<p>최근에 <a href='{_appSettings.WebUrl}'>MyVaccineRecord.CDPH.ca.gov</a>에서 디지털 코로나19 백신 기록을 신청하셨습니다.  안타깝게도 귀하가 제공한 정보는 저희 시스템의 정보와 일치하지 않습니다. 다른 모바일 전화번호나 이메일 주소로 <a href='{_appSettings.WebUrl}'>신청서를 새로 제출</a>하거나 <a href='{_appSettings.VirtualAssistantUrl}'>CDPH 코로나19 가상 도우미</a>에 문의하여 귀하의 기록이 연락처 정보와 연결되도록 도움을 받을 수 있습니다.</p><br/>" +
                            $"<p><b>질문이 있습니까?</b></p>" +
                            $"<p><a href='{_appSettings.VaccineFAQUrl}'>FAQ 페이지에서</a> 디지털 COVID-19 백신 접종 기록에 대해 더 알아보십시오.</p>" +
                            $"<p><b>최신 정보를 얻으십시오.</b></p>" +
                            $"<p><a href='{_appSettings.CovidWebUrl}'>COVID-19 in California에서</a>최신 정보를 보십시오.</p><br/>" +
                            $"<hr>" +
                            $"<footer><p style='text-align:center'>공식 캘리포니아주 행정청 이메일</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>",
                "vi" => $"<img src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png'><br/>" +
                            $"<h3 style='color: #f06724'>Hồ sơ Vắc xin COVID-19 Kỹ thuật số</h3>" +
                            $"<p>Quý vị đang yêu cầu Hồ sơ Vắc xin COVID-19 Kỹ thuật số từ <a href='{_appSettings.WebUrl}'>MyVaccineRecord.CDPH.ca.gov</a>. Rất tiếc, thông tin mà quý vị cung cấp không có trên hệ thống. Quý vị có thể <a href='{_appSettings.WebUrl}'>gửi yêu cầu khác</a> bằng số điện thoại di động hoặc địa chỉ email khác hoặc có thể liên hệ <a href='{_appSettings.VirtualAssistantUrl}'>Trợ lý Ảo CDPH COVID-19</a> để được giúp tìm hồ sơ với thông tin mà quý vị cung cấp.</p><br/>" +
                            $"<p><b>Nếu quý vị có thắc mắc?</b></p>" +
                            $"<p><a href='{_appSettings.VaccineFAQUrl}'>Truy cập trang Các Câu hỏi Thường Gặp</a> để tìm hiểu thêm về Hồ sơ Vắc xin COVID-19 Kỹ thuật số của quý vị.</p>" +
                            $"<p><b>Hãy luôn Cập nhật.</b></p>" +
                            $"<p><a href='{_appSettings.CovidWebUrl}'>Xem thông tin mới nhất</a> về COVID-19 ở California.</p><br/>" +
                            $"<hr>" +
                            $"<footer><p style='text-align:center'>Email chính thức của Tiểu bang California</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>",
                "ae" or "ar" => $"<img src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png' dir='rtl'><br/>" +
                            $"<h3 dir='rtl' style='color: #f06724'>السجل الرقمي للقاح فيروس كورونا (كوفيد-19)</h3>" +
                            $"<p dir='rtl'>قد طلبت مؤخرًا سجل رقمي للقاح كوفيد-19 من <a href='{_appSettings.WebUrl}'>MyVaccineRecord.CDPH.ca.gov</a>. وللأسف، لا تتطابق المعلومات التي قدمتها مع المعلومات الموجودة في نظامنا. يمكنك <a href='{_appSettings.WebUrl}'>إرسال طلب آخر</a> باستخدام رقم هاتف أو عنوان بريد إلكتروني مختلف، أو يمكنك الاتصال <a href='{_appSettings.VirtualAssistantUrl}'>بالمساعد الافتراضي الخاص بكوفيد-19 التابع لإدارة كاليفورنيا للصحة العامة (CDPH)</a> للمساعدة في مطابقة السجل الخاص بك مع معلومات الاتصال الخاصة بك.</p>" +
                            $"<p dir='rtl'><b>هل لديك أي أسئلة؟</b></p>" +
                            $"<p dir='rtl'><a href='{_appSettings.VaccineFAQUrl}'>تفضل بزيارة صفحة الأسئلة الشائعة لدينا</a > لمعرفة المزيد حول سجل اللقاح الرقمي لكوفيد-19 الخاص بك.</p>" +
                            $"<p dir='rtl'><b>ابقَ على اطلاع.</b></p>" +
                            $"<p dir='rtl'><a href='{_appSettings.CovidWebUrl}'>اطلع على أحدث معلومات</a> كوفيد-19 في كاليفورنيا.</p>" +
                            $"<hr>" +
                            $"<footer><p dir='rtl' style='text-align:center'>البريد الإلكتروني الرسمي لوزارة الخارجية الخاص بولاية كاليفورنيا</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>",
                "ph" or "tl" => $"<img src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png'><br/>" +
                            $"<h3 style='color: #f06724'>Digital na Rekord ng Bakuna para sa COVID-19</h3>" +
                            $"<p>Kamakailan kang humiling ng Digital na Rekord ng Pagpapabakuna laban sa COVID-19 mula sa <a href='{_appSettings.WebUrl}'>MyVaccineRecord.CDPH.ca.gov</a>. Sa kasawiang-palad, hindi tumutugma sa impormasyon sa aming system ang impormasyong ibinigay mo. Puwede kang <a href='{_appSettings.WebUrl}'>magsumite ng isa pang kahilingan</a> gamit ang ibang numero ng mobile phone o email address, o puwede kang makipag-ugnayan sa <a href='{_appSettings.VirtualAssistantUrl}'>CDPH COVID-19 Virtual Assistant</a> para sa tulong sa pagtutugma ng iyong rekord sa iyong impormasyon sa pakikipag-ugnayan.</p><br/>" +
                            $"<p><b>May mga tanong?</b></p>" +
                            $"<p><a href='{_appSettings.VaccineFAQUrl}'>Bisitahin ang aming page ng FAQ</a> para alamin pa ang tungkol sa iyong Digital na Record ng Bakuna sa COVID-19.</p>" +
                            $"<p><b>Makibalita.</b></p>" +
                            $"<p><a href='{_appSettings.CovidWebUrl}'>Tingnan ang pinakabagong impormasyon</a> sa COVID-19 sa California.</p><br/>" +
                            $"<hr>" +
                            $"<footer><p style='text-align:center'>Opisyal na Email ng Departamento ng Estado ng California</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>",
                _ => $"<img src='{_appSettings.WebUrl}/imgs/MyTurn-logo.png'><br/>" +
                            $"<h3 style='color: #f06724'>Digital COVID-19 Vaccine Record</h3>" +
                            $"<p>You recently requested a Digital COVID-19 Vaccine Record from <a href='{_appSettings.WebUrl}'>MyVaccineRecord.CDPH.ca.gov</a>. Unfortunately, the information you provided does not match information in our system. " +
                            $"You can <a href='{_appSettings.WebUrl}'>submit another request</a> with a different mobile phone number or email address, or you can contact the <a href='{_appSettings.VirtualAssistantUrl}'>CDPH COVID-19 Virtual Assistant</a> for help in matching your record to your contact information.</p><br/>" +
                            $"<p><b>Have questions?</b></p>" +
                            $"<p><a href='{_appSettings.VaccineFAQUrl}'>Visit our FAQ page</a> to learn more about your Digital COVID-19 Vaccine Record.</p>" +
                            $"<p><b>Stay Informed.</b></p>" +
                            $"<p><a href='{_appSettings.CovidWebUrl}'>View the latest information</a> on COVID-19 in California.</p><br/>" +
                            $"<hr>" +
                            $"<footer><p style='text-align:center'>Official California State Department Email</p>" +
                            $"<p style='text-align:center'><img src='{_appSettings.EmailLogoUrl}'></p></footer>"
            };
        }
        public static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            //consider BILLY BOB
            s = s.ToLower().Trim();
            var tokens = s.Split(" ").ToList();
            var formattedName = "";
            tokens.RemoveAll(b => b.Length == 0);
            foreach (var token in tokens)
            {
                formattedName += char.ToUpper(token[0]) + token[1..] + " ";
            }
            formattedName = formattedName.Trim();
            return formattedName;
        }
        public static bool InPercentRange(int currentMessageCallCount, int percentToVA)
        {
            if ((currentMessageCallCount - 1) % 100 < percentToVA)
            {
                return true;
            }
            return false;
        }


        public static string Sanitize(string text)
        {
            if(text == null)
            {
                text = "";
            }
            var neutralizedString = HttpUtility.UrlEncode(SecurityElement.Escape(text));
            neutralizedString = neutralizedString.Replace("\n", "  ").Replace("\r", "");
            return neutralizedString;
        }
    }
}
