using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

static class Gmail {
    static readonly HttpClient http=new HttpClient();
    static readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=int.MaxValue};
    static string Url64(byte[] b){return Convert.ToBase64String(b).TrimEnd('=').Replace('+','-').Replace('/','_');}
    static string RandomValue(){var b=new byte[32];using(var r=RandomNumberGenerator.Create())r.GetBytes(b);return Url64(b);}
    public static void Import(Preferences p,string file){
        var root=json.Deserialize<Dictionary<string,Dictionary<string,object>>>(File.ReadAllText(file));
        if(!root.ContainsKey("installed"))throw new Exception(I18n.T("Chọn tệp JSON của OAuth client loại Desktop app."));
        p.GoogleId=(string)root["installed"]["client_id"];p.GoogleSecret=(string)root["installed"]["client_secret"];
    }
    static async Task<Dictionary<string,string>> Token(Dictionary<string,string> args){
        using(var body=new FormUrlEncodedContent(args))
        using(var res=await http.PostAsync("https://oauth2.googleapis.com/token",body)){
            if(!res.IsSuccessStatusCode)throw new Exception(I18n.T("Google từ chối cấp quyền. Hãy kiểm tra cấu hình và đăng nhập lại."));
            return json.Deserialize<Dictionary<string,string>>(await res.Content.ReadAsStringAsync());
        }
    }
    public static async Task Login(Preferences p,System.Threading.CancellationToken cancel){
        var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();
        try{
            string redirect="http://127.0.0.1:"+((IPEndPoint)listener.LocalEndpoint).Port+"/";
            string verifier=RandomValue(),state=RandomValue(),challenge;
            using(var sha=SHA256.Create())challenge=Url64(sha.ComputeHash(Encoding.ASCII.GetBytes(verifier)));
            var query=HttpUtility.ParseQueryString("");
            query["client_id"]=p.GoogleId;query["redirect_uri"]=redirect;query["response_type"]="code";query["scope"]="https://www.googleapis.com/auth/gmail.send";query["access_type"]="offline";query["prompt"]="consent select_account";query["state"]=state;query["code_challenge"]=challenge;query["code_challenge_method"]="S256";
            Process.Start("https://accounts.google.com/o/oauth2/v2/auth?"+query);
            var deadline=Task.Delay(TimeSpan.FromMinutes(4),cancel);
            while(true){
                var accept=listener.AcceptTcpClientAsync();if(await Task.WhenAny(accept,deadline)!=accept)throw new Exception(I18n.T("Đã hết thời gian đăng nhập."));
                using(var client=await accept)using(var stream=client.GetStream())using(var reader=new StreamReader(stream)){
                    var read=reader.ReadLineAsync();if(await Task.WhenAny(read,deadline)!=read)throw new Exception(I18n.T("Đã hết thời gian đăng nhập."));
                    var pieces=(await read??"").Split(' ');if(pieces.Length!=3||pieces[0]!="GET"||!pieces[1].StartsWith("/?"))continue;
                    var response=HttpUtility.ParseQueryString(pieces[1].Substring(2));if(response["state"]!=state)continue;
                    var reply=Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nConnection: close\r\n\r\nReturn to LockScreen to continue.");await stream.WriteAsync(reply,0,reply.Length);
                    if(string.IsNullOrEmpty(response["code"]))throw new Exception(I18n.T("Bạn chưa cấp quyền gửi thư."));
                    var tokens=await Token(new Dictionary<string,string>{{"client_id",p.GoogleId},{"client_secret",p.GoogleSecret},{"code",response["code"]},{"redirect_uri",redirect},{"grant_type","authorization_code"},{"code_verifier",verifier}});
                    p.Refresh=tokens["refresh_token"];return;
                }
            }
        }finally{listener.Stop();}
    }
    public static async Task Send(Preferences p,string path){
        var tokens=await Token(new Dictionary<string,string>{{"client_id",p.GoogleId},{"client_secret",p.GoogleSecret},{"refresh_token",p.Refresh},{"grant_type","refresh_token"}});
        var address=new System.Net.Mail.MailAddress(p.To).Address;
        if(address.Contains("\r")||address.Contains("\n"))throw new Exception(I18n.T("Địa chỉ nhận không hợp lệ."));
        string boundary=Guid.NewGuid().ToString("N");
        string mime="To: "+address+"\r\nSubject: LockScreen - "+(path==null?"Thu thu":"Canh bao dang nhap")+"\r\nMIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=\""+boundary+"\"\r\n\r\n--"+boundary+"\r\nContent-Type: text/plain; charset=utf-8\r\n\r\n"+(path==null?I18n.T("Gửi thư thử thành công."):I18n.T("Có lần nhập sai mật khẩu. Xem ảnh đính kèm."))+"\r\n";
        if(path!=null)mime+="--"+boundary+"\r\nContent-Type: image/jpeg\r\nContent-Disposition: attachment; filename=\""+Path.GetFileName(path)+"\"\r\nContent-Transfer-Encoding: base64\r\n\r\n"+Convert.ToBase64String(File.ReadAllBytes(path),Base64FormattingOptions.InsertLineBreaks)+"\r\n";
        mime+="--"+boundary+"--\r\n";
        using(var req=new HttpRequestMessage(HttpMethod.Post,"https://gmail.googleapis.com/gmail/v1/users/me/messages/send")){
            req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",tokens["access_token"]);
            req.Content=new StringContent(json.Serialize(new{raw=Url64(Encoding.UTF8.GetBytes(mime))}),Encoding.UTF8,"application/json");
            using(var res=await http.SendAsync(req)){if(!res.IsSuccessStatusCode)throw new Exception(I18n.T("Gmail từ chối gửi (")+(int)res.StatusCode+I18n.T("). Kiểm tra Gmail API, quyền gửi thư và địa chỉ nhận."));}
        }
    }
}



