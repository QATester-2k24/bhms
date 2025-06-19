$(document).ready(function(){

    $('#changePassword').on('submit', function(e){
        e.preventDefault();
        var form = $(this);
        $.ajax({
            type: "POST",
            url: "/dashboard/updatePassword",
            dataType : "json",
            cache : false,
            data : form.serialize(),
            beforeSend: function(){
                toastr["info"]("Submiting Form");
                loaderShow('main_loader');
            }
        }).done(function(response){
            if(response.status == 'success'){
                showErrorAlert('changePassword_alert', 'alert-success', response.msg);
                toastr["success"](response.msg);

                setTimeout(function(){
                    window.location.href = APP_URL+"/auth/logout";
                }, 5000);
                
            }else{
                var errors = '';
                $.each( response.errors, function( key, value ){
                    toastr["error"](value);
                    errors += value+"<br>";
                });
                showErrorAlert('changePassword_alert', 'alert-danger', errors);
            }
        }).always(function(){
            loaderHide('main_loader');
        });
    });

    
    jQuery.validator.addMethod("hasCaps", function(value, element) {
        var hasCaps = /[A-Z]/.test(value);
        return hasCaps;
    },"Have at least one uppercase letter" );

    jQuery.validator.addMethod("hasNums", function(value, element) {
        var hasNums = /\d/.test(value);
        return hasNums;
    },"Have at least one number" );

    jQuery.validator.addMethod("hasSpecials", function(value, element) {
        var hasSpecials = /[~!,@#%&_\$\^\*\?\-]/.test(value);
        return hasSpecials;
    },"Special characters e.g. @ # etc. are mandatory" );

    jQuery.validator.addMethod( "nowhitespace", function( value, element ) {
        return this.optional(element) ||  /^\S+$/i.test( value );
    }, "No white space please" );

    jQuery.validator.addMethod("consecutive4Letters", function(value, element) { 
        return this.optional(element) ||  !/([a-z\d])\1\1\1/i.test(value); 
    }, "Do not have consecutive 4 letters");

    jQuery('#changePassword').validate({
        rules : {
            new_password : {
                required: true,
                minlength : 8,
                maxlength : 12,
                hasCaps: true,
                hasNums: true,
                hasSpecials :true,
                nowhitespace: true,
                consecutive4Letters: true
            },
            confirm_password : {
                minlength : 8,
                equalTo : "#new_password",
                maxlength : 12,
                hasCaps: true,
                hasNums: true,
                hasSpecials :true,
                nowhitespace: true,
                consecutive4Letters: true
            }
        }
    });

});