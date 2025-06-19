$(document).ready(function(){

    $('#edit').hide();

    $('#card_no').on('change', function(){

        if($(this).val() == null || $(this).val() == ''){
            toastr["error"]('Card Field is Empty');
            $('[type=submit]').prop( "disabled", true );
            return false;
        }

        $.ajax({
            type: "GET",
            url: "/app_user/store/getdetails/"+$(this).val(),
            dataType : "json",
            cache : false,
            beforeSend: function(){
                toastr["info"]("Getting Details");
                loaderShow('main_loader');
            }
        }).done(function(response){
            if(response.status == 'success'){
                toastr["success"](response.msg);
                $('#pUsername').html(response.UNAME);
                $('#P_R_date_of_birth').html(response.DOB);

                $('[name=username]').val(response.UNAME);
                $('[name=date_of_birth]').val(response.DOB);

                $('[type=submit]').prop( "disabled", false );
                $('#edit').hide();

            }else if(response.status == 'duplicate'){
                var edit_url = '/app_user/edit/';

                toastr["success"](response.msg);
                $('#pUsername').html(response.UNAME);
                $('#P_R_date_of_birth').html(response.DOB);
                $('[type=submit]').prop( "disabled", true );
                $('#edit').show();
                $('#edit').prop('href', edit_url+response.userinfo.id);
                showErrorAlert('appUser_alert', 'alert-info', response.msg);

            }else{
                toastr["error"](response.msg);
                $('[type=submit]').prop( "disabled", true );
                $('#pUsername').html('.................');
                $('#P_R_date_of_birth').html('../../....');
                $('#edit').hide();
                showErrorAlert('appUser_alert', 'alert-danger', response.msg);
            
            }
        }).always(function(){
            loaderHide('main_loader');
        });
    });

    $('#create_appuser').on('submit', function(e){
        e.preventDefault();
        var form = $(this);
        $.ajax({
            type: "POST",
            url: "/app_user/create",
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
                var errors = '';
                if (response.errors) {
                    $.each( response.errors, function( key, value ){
                        toastr["error"](value);
                        errors += value+"<br>";
                    });
                    showErrorAlert('appUser_alert', 'alert-danger', errors);   
                }else{
                    showErrorAlert('appUser_alert', 'alert-danger', response.msg);
                }
            }
        }).always(function(){
            loaderHide('main_loader');
        });
    });
});