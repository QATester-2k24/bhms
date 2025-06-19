$(document).ready(function(){

    $("#forget_pass").click(function (e) {
        e.preventDefault();
        $('#login_credentials').slideToggle("slow");
        $('#forget_password').removeClass('d-none');
    });

    $('#login_credentials').on('submit', function(e){
        e.preventDefault();
        var form = $(this);
        $.ajax({
            type: "POST",
            url: "/auth/authenticate",
            dataType : "json",
            cache : false,
            data : form.serialize(),
            beforeSend: function(){
                toastr["info"]("Submiting Form");
                loaderShow('login_loader');

            },success: function(response){
                loaderHide('login_loader');

                if(response.status == 'success'){
                    showAlert('login_alert', 'alert-success', response.msg);
                    toastr["success"](response.msg);
                    window.location.href = APP_URL+response.redirectUrl;
                }else{
                    showAlert('login_alert', 'alert-danger', response.msg);
                    toastr["error"](response.msg);
                }

            },error : function(){
                loaderHide('login_loader');
                toastr["error"]("Some Error Occured");
                showAlert('login_alert', 'alert-danger', 'Some Error Occured');
            }
        });
    });

    $('#forget_password').on('submit', function(e){
        e.preventDefault();
        var form = $(this);
        
        $.ajax({
            type: "POST",
            url: "auth/forgetPassword",
            dataType: "json",
            cache : false,
            data: form.serialize(),
            beforeSend: function(){
                toastr['info']('Submiting Form');
                loaderShow('login_loader');
            },success: function(response){
                loaderHide('login_loader');

                if(response.status == 'success'){
                    showAlert('forget_password_alert', 'alert-success', response.msg);
                    toastr['success'](response.msg);
                }else{
                    showAlert('forget_password_alert', 'alert-danger', response.msg);
                    toastr['error'](response.msg);
                }
            },error: function(){
                loaderHide('login_loader');
                toastr['error']('Some Error Occured');
                showAlert('forget_password_alert', 'alert-danger', 'Some Error Occured');
            }
        });

    });

});