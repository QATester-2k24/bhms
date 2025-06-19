$(document).ready(function(){

    const allowedDomains = [ 'jubileelife.com', 'jubileehealthclaims.com' ]; 
    $('#email').on('change', function(){
        var email = $(this).val();
        var domain = email.substring(email.lastIndexOf("@") +1);
        
        if (!allowedDomains.includes(domain)) {
            $(this).val('');
            toastr["error"]('Given Domain Not Allowed');
        }
    });


    $('#create_user').on('submit', function(e){
        e.preventDefault();
        var form = $(this);
        $.ajax({
            type: "POST",
            url: "/system_user/store",
            dataType : "json",
            cache : false,
            data : form.serialize(),
            beforeSend: function(){
                toastr["info"]("Submiting Form");
                loaderShow('main_loader');
            }
        }).done(function(response){
            if(response.status == 'success'){
                showErrorAlert('systemUser_alert', 'alert-success', response.msg);
                toastr["success"](response.msg);
            }else{
                var errors = '';
                $.each( response.errors, function( key, value ){
                    toastr["error"](value);
                    errors += value+"<br>";
                });
                showErrorAlert('systemUser_alert', 'alert-danger', errors);
            }
        }).always(function(){
            loaderHide('main_loader');
        });
    });

});