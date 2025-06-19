$(document).ready(function(){

    $('#edit_appuser').on('submit', function(e){
        e.preventDefault();

        var form = $(this);
        var edit_id = $(this).attr('data-userid');

        $.ajax({
            type: "POST",
            url: "/app_user/update/"+edit_id,
            dataType : "json",
            cache : false,
            data : form.serialize(),
            beforeSend: function(){
                toastr["info"]("Submiting Form");
                loaderShow('main_loader');
            }
        }).done(function(response){
            if(response.status == 'success'){
                showErrorAlert('appUser_alert', 'alert-success', response.msg);
                toastr["success"](response.msg);
            }else{
                showErrorAlert('appUser_alert', 'alert-danger', response.msg);
                toastr["error"](response.msg);
            }
        }).always(function(){
            loaderHide('main_loader');
        });
    });


    $('#change_password').on('click', function(e){
        e.preventDefault();

        var form = $('#edit_appuser');
        var edit_id = $('#edit_appuser').attr('data-userid');

        $.ajax({
            type: "POST",
            url: "/app_user/resetPassword/"+edit_id,
            dataType : "json",
            cache : false,
            data : form.serialize(),
            beforeSend: function(){
                toastr["info"]("Submiting Form");
                loaderShow('main_loader');
            }
        }).done(function(response){
            if(response.status == 'success'){
                showErrorAlert('appUser_alert', 'alert-success', response.msg);
                toastr["success"](response.msg);
            }else{
                showErrorAlert('appUser_alert', 'alert-danger', response.msg);
                toastr["error"](response.msg);
            }
        }).always(function(){
            loaderHide('main_loader');
        });


    });

});